using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Ked.Presentation.Core.Tests
{
    /// <summary>
    /// b-5 depth 묶음 등가성: 새 순수 solver(SettledFocusMath)가
    /// 종전의 "임시 적용→측정→복원" 알고리즘과 같은 값을 내는지를,
    /// 종전 알고리즘을 오라클로 이 테스트 안에 재현해 실제 RectTransform 위에서 비교한다.
    /// </summary>
    public sealed class DepthFocusUnityParityTests
    {
        private const float Eps = 0.01f;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private RectTransform _stage;       // RigSpaceRoot 상당
        private RectTransform _trackFocus;  // place의 이동 축
        private RectTransform _depthY;
        private RectTransform _depthScale;
        private RectTransform _track;
        private RectTransform _visualOffset; // focus 측정 노드

        [SetUp]
        public void SetUp()
        {
            _stage = CreateRect("Stage", null, r =>
            {
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(1920f, 1080f);
            });

            _trackFocus = CreateStretchChild("TrackFocus", _stage);
            _trackFocus.anchoredPosition = new Vector2(-80f, 40f);

            _depthY = CreateStretchChild("DepthY", _trackFocus);
            _depthY.anchoredPosition = new Vector2(0f, 120f);

            _depthScale = CreateStretchChild("DepthScale", _depthY);
            _depthScale.pivot = new Vector2(0.5f, 0f);
            _depthScale.localScale = new Vector3(0.86f, 0.86f, 1f);

            _track = CreateStretchChild("Track", _depthScale);
            _track.anchoredPosition = new Vector2(240f, -30f);

            _visualOffset = CreateStretchChild("VisualOffset", _track);
            _visualOffset.pivot = new Vector2(0.5f, 0f);
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

        // ── depth 보존 보정 ──────────────────────────────────────────

        [Test]
        public void 새_depth_보정이_종전_임시적용_측정과_같다()
        {
            CharacterPlacementTargetLedger ledger = new CharacterPlacementTargetLedger();
            // 진행 중 트윈의 정착 예약이 있는 상태도 겹쳐 본다 (실전과 같은 조건).
            ledger.PublishAnchoredPosition(_track, new Vector2(-160f, 40f));

            Vector3 focusOffset = new Vector3(10f, 350f, 0f);
            Vector2 rawDepthY = new Vector2(0f, -320f);
            Vector2 targetScale = new Vector2(1.18f, 1.18f);

            Vector2 oracle = OracleDepthCompensation(ledger, focusOffset, rawDepthY, targetScale);

            List<RectTransform> chainRects = new();
            RectNodeState[] chain = ledger.CaptureSettledChain(_visualOffset, _stage, chainRects);

            Vec2 solved = SettledFocusMath.SolveDepthYPreservingFocus(
                chain,
                CharacterPlacementTargetLedger.SpaceOf(_stage),
                chainRects.IndexOf(_depthY),
                chainRects.IndexOf(_depthScale),
                new Vec2(focusOffset.x, focusOffset.y),
                new Vec2(rawDepthY.x, rawDepthY.y),
                new Vec2(targetScale.x, targetScale.y));

            Assert.That(solved.X, Is.EqualTo(oracle.x).Within(Eps), "depthY X");
            Assert.That(solved.Y, Is.EqualTo(oracle.y).Within(Eps), "depthY Y");
        }

        [Test]
        public void depth_보정_계산이_리그를_바꾸지_않는다()
        {
            CharacterPlacementTargetLedger ledger = new CharacterPlacementTargetLedger();
            ledger.PublishAnchoredPosition(_track, new Vector2(-160f, 40f));

            string before = SnapshotHierarchy();

            List<RectTransform> chainRects = new();
            RectNodeState[] chain = ledger.CaptureSettledChain(_visualOffset, _stage, chainRects);

            SettledFocusMath.SolveDepthYPreservingFocus(
                chain,
                CharacterPlacementTargetLedger.SpaceOf(_stage),
                chainRects.IndexOf(_depthY),
                chainRects.IndexOf(_depthScale),
                new Vec2(0f, 350f),
                new Vec2(0f, 440f),
                new Vec2(1.38f, 1.38f));

            Assert.That(SnapshotHierarchy(), Is.EqualTo(before));
        }

        // ── place 배치 ───────────────────────────────────────────────

        [Test]
        public void 새_place_배치가_종전_변환_공식과_같다()
        {
            CharacterPlacementTargetLedger ledger = new CharacterPlacementTargetLedger();

            Vector3 focusOffset = new Vector3(0f, 350f, 0f);
            Vector2 desired = new Vector2(-460.8f, 0f); // Left 상당

            Vector2 oracle = OraclePlaceDestination(ledger, focusOffset, desired);

            List<RectTransform> chainRects = new();
            RectNodeState[] chain = ledger.CaptureSettledChain(_visualOffset, _stage, chainRects);

            Vec2 solved = SettledFocusMath.SolveFocusPlacement(
                chain,
                CharacterPlacementTargetLedger.SpaceOf(_stage),
                chainRects.IndexOf(_trackFocus),
                new Vec2(focusOffset.x, focusOffset.y),
                new Vec2(desired.x, desired.y),
                new Vec2(_trackFocus.anchoredPosition.x, _trackFocus.anchoredPosition.y));

            Assert.That(solved.X, Is.EqualTo(oracle.x).Within(Eps), "place X");
            Assert.That(solved.Y, Is.EqualTo(oracle.y).Within(Eps), "place Y");
        }

        // ── 오라클: 종전 알고리즘 그대로 ─────────────────────────────

        /// <summary>종전 CalculateDepthYThatPreservesCurrentFocus: 임시 적용→측정→복원.</summary>
        private Vector2 OracleDepthCompensation(
            CharacterPlacementTargetLedger ledger,
            Vector3 focusOffset,
            Vector2 rawDepthY,
            Vector2 targetScale)
        {
            Vector3 currentWorld = ledger.MeasureSettledWorldPoint(_visualOffset, focusOffset, _stage);
            Vector2 currentFocus = ToRigSpace(currentWorld);

            Vector2 savedDepthY = _depthY.anchoredPosition;
            Vector3 savedScale = _depthScale.localScale;

            _depthY.anchoredPosition = rawDepthY;
            _depthScale.localScale = new Vector3(targetScale.x, targetScale.y, savedScale.z);

            Vector3 targetWorld = ledger.MeasureSettledWorldPoint(_visualOffset, focusOffset, _stage);
            Vector2 targetFocus = ToRigSpace(targetWorld);

            _depthScale.localScale = savedScale;
            _depthY.anchoredPosition = savedDepthY;

            Vector2 compensation = PresentationCoordinateMath
                .ConvertVectorFromRigSpaceToTargetPositionParentSpace(
                    currentFocus - targetFocus,
                    _stage,
                    _depthY.parent as RectTransform);

            return rawDepthY + compensation;
        }

        /// <summary>종전 CharacterFocusPlacementSolver: 점 변환 두 번 → 델타 → 현재 위치에 가산.</summary>
        private Vector2 OraclePlaceDestination(
            CharacterPlacementTargetLedger ledger,
            Vector3 focusOffset,
            Vector2 desiredInRigSpace)
        {
            Vector3 currentWorld = ledger.MeasureSettledWorldPoint(_visualOffset, focusOffset, _stage);
            Vector2 currentFocus = ToRigSpace(currentWorld);

            RectTransform targetParent = _trackFocus.parent as RectTransform;

            Vector2 currentInParent = PresentationCoordinateMath
                .ConvertPointFromRigSpaceToTargetPositionParentSpace(currentFocus, _stage, targetParent);

            Vector2 desiredInParent = PresentationCoordinateMath
                .ConvertPointFromRigSpaceToTargetPositionParentSpace(desiredInRigSpace, _stage, targetParent);

            return _trackFocus.anchoredPosition + (desiredInParent - currentInParent);
        }

        // ── helper ───────────────────────────────────────────────────

        private Vector2 ToRigSpace(Vector3 world)
        {
            Vector3 local = _stage.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

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
                    $"euler={rect.localEulerAngles:F6}");

                for (int i = 0; i < rect.childCount; i++)
                {
                    if (rect.GetChild(i) is RectTransform child)
                        Walk(child);
                }
            }

            Walk(_stage);
            return sb.ToString();
        }
    }
}
