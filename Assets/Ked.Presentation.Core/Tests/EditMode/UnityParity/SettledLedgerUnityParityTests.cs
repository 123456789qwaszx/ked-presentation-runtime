using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Ked.Presentation.Core.Tests
{
    /// <summary>
    /// b-3 등가성 + 무대 불변 고정.
    ///
    /// 새 CharacterPlacementTargetLedger(순수 계산)가 종전의
    /// "부모들을 target 값으로 잠깐 세팅 → 측정 → 복원" 구현과 같은 값을 내는지를,
    /// 종전 알고리즘을 오라클로 이 테스트 안에 그대로 재현해 비교한다.
    ///
    /// 그리고 측정이 유니티에 아무것도 쓰지 않는다는 것(예외 경로 포함)을 고정한다 —
    /// b-3의 수용 기준 "측정 중 예외가 나도 리그 상태가 변하지 않는다".
    /// </summary>
    public sealed class SettledLedgerUnityParityTests
    {
        // 종전 구현과의 대조이므로 b-1 하네스와 같은 ε.
        private const float Eps = 0.01f;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private RectTransform _stage;      // stopRoot (RigSpaceRoot 상당)
        private RectTransform _depthY;     // 위치 예약이 실리는 노드 (SetDepth의 _depthYRect 상당)
        private RectTransform _depthScale; // 스케일 예약 노드 (바닥 pivot)
        private RectTransform _track;      // 위치 예약 노드 (MoveBy의 _rect 상당)
        private RectTransform _measure;    // 측정 노드 (CharacterPortrait_VisualOffset 상당)

        [SetUp]
        public void SetUp()
        {
            // 실제 캐릭터 리그의 축 구조를 축약한 모양: 전부 스트레치 풀,
            // DepthScale은 바닥 pivot + 스케일. 실사용과 같은 "종류당 노드 하나".
            _stage = CreateRect("Stage", null, r =>
            {
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(1920f, 1080f);
            });

            _depthY = CreateStretchChild("DepthY", _stage);
            _depthY.anchoredPosition = new Vector2(0f, 120f);

            _depthScale = CreateStretchChild("DepthScale", _depthY);
            _depthScale.pivot = new Vector2(0.5f, 0f);
            _depthScale.localScale = new Vector3(1.1f, 1.1f, 1f);

            _track = CreateStretchChild("Track", _depthScale);
            _track.anchoredPosition = new Vector2(240f, 0f);

            _measure = CreateStretchChild("VisualOffset", _track);
            _measure.pivot = new Vector2(0.5f, 0f);
            _measure.anchoredPosition = new Vector2(0f, 30f);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);
            }

            _spawned.Clear();
        }

        private CharacterPlacementTargetLedger MakeLedgerWithRealUsagePattern()
        {
            // 실제 커맨드들의 게시 패턴 그대로:
            // SetDepth → depthY 위치 + depthScale 스케일, MoveBy → track 위치.
            CharacterPlacementTargetLedger ledger = new CharacterPlacementTargetLedger();
            ledger.PublishAnchoredPosition(_depthY, new Vector2(0f, -320f));
            ledger.PublishLocalScale(_depthScale, new Vector2(1.38f, 1.38f));
            ledger.PublishAnchoredPosition(_track, new Vector2(-160f, 40f));
            return ledger;
        }

        // ── 등가성: 종전 알고리즘(오라클) vs 새 순수 계산 ─────────────

        [Test]
        public void MeasureSettledWorldPoint가_종전_구현과_같다()
        {
            CharacterPlacementTargetLedger ledger = MakeLedgerWithRealUsagePattern();

            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(12.3f, 45.6f, 0f),
                new Vector3(-78.9f, -12.3f, 0f),
            };

            foreach (Vector3 offset in offsets)
            {
                Vector3 oracle = OracleMeasureSettledWorldPoint(_measure, offset, _stage);
                Vector3 pure = ledger.MeasureSettledWorldPoint(_measure, offset, _stage);

                AssertNear(oracle, pure, $"Measure({offset})");
            }
        }

        [Test]
        public void WorldPointToSettledParentLocalPoint가_종전_구현과_같다()
        {
            CharacterPlacementTargetLedger ledger = MakeLedgerWithRealUsagePattern();

            Vector3[] worldPoints =
            {
                _stage.TransformPoint(new Vector3(100f, -200f, 0f)),
                _stage.TransformPoint(Vector3.zero),
                _stage.TransformPoint(new Vector3(-640f, 360f, 0f)),
            };

            foreach (Vector3 world in worldPoints)
            {
                Vector2 oracle = OracleWorldPointToSettledParentLocalPoint(_track, world, _stage);
                Vector2 pure = ledger.WorldPointToSettledParentLocalPoint(_track, world, _stage);

                Assert.That(pure.x, Is.EqualTo(oracle.x).Within(Eps), $"Inverse({world}) X");
                Assert.That(pure.y, Is.EqualTo(oracle.y).Within(Eps), $"Inverse({world}) Y");
            }
        }

        [Test]
        public void 회전_예약도_종전_구현과_같다()
        {
            CharacterPlacementTargetLedger ledger = new CharacterPlacementTargetLedger();
            ledger.PublishLocalEuler(_track, new Vector3(0f, 0f, 25f));
            ledger.PublishAnchoredPosition(_depthY, new Vector2(50f, -100f));

            Dictionary<RectTransform, List<OracleEntry>> oracleTargets = new()
            {
                [_track] = new List<OracleEntry>
                    { new OracleEntry(OracleKind.LocalEuler, new Vector3(0f, 0f, 25f)) },
                [_depthY] = new List<OracleEntry>
                    { new OracleEntry(OracleKind.AnchoredPosition, new Vector3(50f, -100f, 0f)) },
            };

            Vector3 offset = new Vector3(30f, 40f, 0f);

            Vector3 oracle = OracleMeasureSettledWorldPoint(_measure, offset, _stage, oracleTargets);
            Vector3 pure = ledger.MeasureSettledWorldPoint(_measure, offset, _stage);

            AssertNear(oracle, pure, "Rotation");
        }

        [Test]
        public void 예약이_없으면_라이브_측정과_같다()
        {
            CharacterPlacementTargetLedger ledger = new CharacterPlacementTargetLedger();

            Vector3 offset = new Vector3(5f, 6f, 0f);

            AssertNear(
                _measure.TransformPoint(offset),
                ledger.MeasureSettledWorldPoint(_measure, offset, _stage),
                "Empty ledger");
        }

        // ── 무대 불변: 측정은 아무것도 쓰지 않는다 ───────────────────

        [Test]
        public void 측정은_리그_상태를_바꾸지_않는다()
        {
            CharacterPlacementTargetLedger ledger = MakeLedgerWithRealUsagePattern();

            string before = SnapshotHierarchy();

            ledger.MeasureSettledWorldPoint(_measure, new Vector3(1f, 2f, 0f), _stage);
            ledger.WorldPointToSettledParentLocalPoint(_track, Vector3.one, _stage);

            Assert.That(SnapshotHierarchy(), Is.EqualTo(before));
        }

        [Test]
        public void 측정_중_예외가_나도_리그_상태가_변하지_않는다()
        {
            CharacterPlacementTargetLedger ledger = MakeLedgerWithRealUsagePattern();

            // stopRoot가 조상이 아닌 잘못된 호출 — 조용히 어긋나는 대신 예외.
            RectTransform stranger = CreateRect("Stranger", null, r => { });

            string before = SnapshotHierarchy();

            Assert.Throws<System.ArgumentException>(
                () => ledger.MeasureSettledWorldPoint(_measure, Vector3.zero, stranger));

            Assert.That(SnapshotHierarchy(), Is.EqualTo(before));
        }

        // ── 오라클: 종전 구현 그대로 (적용 → 측정 → 복원) ─────────────

        private enum OracleKind { AnchoredPosition, LocalScale, LocalEuler }

        private readonly struct OracleEntry
        {
            public readonly OracleKind Kind;
            public readonly Vector3 Value;

            public OracleEntry(OracleKind kind, Vector3 value)
            {
                Kind = kind;
                Value = value;
            }
        }

        /// <summary>이 테스트의 예약과 같은 내용의 종전식 장부.</summary>
        private Dictionary<RectTransform, List<OracleEntry>> OracleTargets()
        {
            // 종전 구현은 노드당 엔트리 하나였다. 실사용이 노드당 종류 하나이므로
            // (이 테스트의 예약 패턴도 그렇다) 리스트에는 항상 하나만 들어간다.
            return new Dictionary<RectTransform, List<OracleEntry>>
            {
                [_depthY] = new List<OracleEntry>
                    { new OracleEntry(OracleKind.AnchoredPosition, new Vector3(0f, -320f, 0f)) },
                [_depthScale] = new List<OracleEntry>
                    { new OracleEntry(OracleKind.LocalScale, new Vector3(1.38f, 1.38f, 0f)) },
                [_track] = new List<OracleEntry>
                    { new OracleEntry(OracleKind.AnchoredPosition, new Vector3(-160f, 40f, 0f)) },
            };
        }

        private Vector3 OracleMeasureSettledWorldPoint(
            RectTransform measureRect, Vector3 localOffset, RectTransform stopRoot,
            Dictionary<RectTransform, List<OracleEntry>> targets = null)
            => OracleMeasure(measureRect, stopRoot, () => measureRect.TransformPoint(localOffset), targets);

        private Vector2 OracleWorldPointToSettledParentLocalPoint(
            RectTransform parentRect, Vector3 worldPoint, RectTransform stopRoot,
            Dictionary<RectTransform, List<OracleEntry>> targets = null)
        {
            Vector3 local = OracleMeasure(parentRect, stopRoot,
                () => parentRect.InverseTransformPoint(worldPoint), targets);

            return new Vector2(local.x, local.y);
        }

        private Vector3 OracleMeasure(
            RectTransform startRect, RectTransform stopRoot, System.Func<Vector3> measure,
            Dictionary<RectTransform, List<OracleEntry>> targets = null)
        {
            targets ??= OracleTargets();

            List<RectTransform> touched = new List<RectTransform>();
            List<OracleEntry> saved = new List<OracleEntry>();

            Transform current = startRect;

            while (current != null && current != stopRoot)
            {
                if (current is RectTransform rect && targets.TryGetValue(rect, out List<OracleEntry> entries))
                {
                    foreach (OracleEntry entry in entries)
                    {
                        touched.Add(rect);
                        saved.Add(CaptureOracle(rect, entry.Kind));
                        ApplyOracle(rect, entry);
                    }
                }

                current = current.parent;
            }

            Vector3 result = measure();

            for (int i = touched.Count - 1; i >= 0; i--)
                ApplyOracle(touched[i], saved[i]);

            return result;
        }

        private static OracleEntry CaptureOracle(RectTransform rect, OracleKind kind)
        {
            switch (kind)
            {
                case OracleKind.AnchoredPosition:
                    Vector2 ap = rect.anchoredPosition;
                    return new OracleEntry(kind, new Vector3(ap.x, ap.y, 0f));
                case OracleKind.LocalScale:
                    return new OracleEntry(kind, rect.localScale);
                default:
                    return new OracleEntry(kind, rect.localEulerAngles);
            }
        }

        private static void ApplyOracle(RectTransform rect, OracleEntry entry)
        {
            switch (entry.Kind)
            {
                case OracleKind.AnchoredPosition:
                    rect.anchoredPosition = new Vector2(entry.Value.x, entry.Value.y);
                    break;
                case OracleKind.LocalScale:
                    Vector3 s = rect.localScale;
                    rect.localScale = new Vector3(entry.Value.x, entry.Value.y, s.z);
                    break;
                case OracleKind.LocalEuler:
                    rect.localEulerAngles = entry.Value;
                    break;
            }
        }

        // ── helper ───────────────────────────────────────────────────

        private RectTransform CreateRect(string name, RectTransform parent, System.Action<RectTransform> setup)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));

            if (parent == null)
                _spawned.Add(go);

            RectTransform rt = (RectTransform)go.transform;

            if (parent != null)
                rt.SetParent(parent, false);

            setup(rt);
            return rt;
        }

        private RectTransform CreateStretchChild(string name, RectTransform parent)
        {
            return CreateRect(name, parent, r =>
            {
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
            });
        }

        private string SnapshotHierarchy()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            void Walk(RectTransform rect)
            {
                sb.AppendLine(
                    $"{rect.name}: ap={rect.anchoredPosition:F6} scale={rect.localScale:F6} " +
                    $"euler={rect.localEulerAngles:F6} sd={rect.sizeDelta:F6}");

                for (int i = 0; i < rect.childCount; i++)
                {
                    if (rect.GetChild(i) is RectTransform child)
                        Walk(child);
                }
            }

            Walk(_stage);
            return sb.ToString();
        }

        private static void AssertNear(Vector3 expected, Vector3 actual, string context)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Eps), $"{context} X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Eps), $"{context} Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Eps), $"{context} Z");
        }
    }
}
