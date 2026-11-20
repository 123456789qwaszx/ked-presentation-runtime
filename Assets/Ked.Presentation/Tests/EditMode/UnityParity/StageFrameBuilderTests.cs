using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests.UnityParity
{
    /// <summary>
    /// 무대 틀(창)은 무대 루트 바깥에서 리그를 감싼다 — StageFrameBuilder. 컷인 판 · 팝업 그림은 무대 안의 리그다.
    /// 리그만 세운 계층은 StageRigBuilderTests.
    /// </summary>
    public sealed class StageFrameBuilderTests
    {
        private readonly List<GameObject> _spawned = new();
        private RectTransform _mount;
        private StageRigBuilder _rigBuilder;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new("__Mount", typeof(RectTransform));
            _spawned.Add(go);
            _mount = (RectTransform)go.transform;
            _mount.sizeDelta = new Vector2(1920f, 1080f);
            _rigBuilder = new StageRigBuilder();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _spawned.Clear();
        }

        private StageRigRefs BuildRig(string stageKey) => _rigBuilder.Build(_mount, stageKey, StageLayerLabels.Depths);

        // ── 창 · 컷인 (docs/PLAN_cutin.md) ──────────────────────────

        [Test]
        public void 창이_무대_루트를_감싸고_창_아래는_무대_루트뿐이다()
        {
            StageRigRefs rig = BuildRig("stage00");
            StageCutInSlot cutIn = StageFrameBuilder.Build(rig);

            Assert.That(rig.Root.parent, Is.EqualTo(cutIn.Window), "무대 루트는 창 아래");
            Assert.That(cutIn.Window.parent, Is.EqualTo(_mount));
            Assert.That(cutIn.Window.name, Is.EqualTo("stage00_Window"));
            Assert.That(cutIn.Window.childCount, Is.EqualTo(1), "컷인 판 그림은 틀이 아니라 무대 안의 리그다(docs/PLAN_stage-visibility.md)");
        }

        [Test]
        public void 창_아래의_옛_판_노드는_경고하고_치운다()
        {
            StageRigRefs rig = BuildRig("stage01");
            RectTransform window = StageFrameBuilder.Build(rig).Window;
            NewChild(window, "stage01_Panel");

            LogAssert.Expect(LogType.Warning, new Regex("창 아래에 스키마에 없는 'stage01_Panel'"));
            StageFrameBuilder.Build(_rigBuilder.BindRefsFromRoot(rig.Root, "stage01"));

            Assert.That(window.childCount, Is.EqualTo(1));
        }

        [Test]
        public void 옛_씬의_무대_루트는_부수지_않고_창으로_감싼다()
        {
            // 틀 이전 계층: mount 바로 아래 무대 루트. 다른 무대 둘 사이의 그리기 순서를 지킨다.
            RectTransform before = NewChild(_mount, "Before");
            StageRigRefs rig = BuildRig("stage01");
            RectTransform after = NewChild(_mount, "After");

            RectTransform pan = rig.Camera.Pan;
            StageCutInSlot cutIn = StageFrameBuilder.Build(rig);

            Assert.That(rig.Camera.Pan, Is.SameAs(pan), "무대 계층은 그대로");
            Assert.That(rig.Root.parent, Is.EqualTo(cutIn.Window));
            Assert.That(cutIn.Window.parent, Is.EqualTo(_mount));
            Assert.That(before.GetSiblingIndex(), Is.LessThan(cutIn.Window.GetSiblingIndex()));
            Assert.That(after.GetSiblingIndex(), Is.GreaterThan(cutIn.Window.GetSiblingIndex()));
        }

        [Test]
        public void 다시_묶으면_있는_창을_쓰고_리그도_창_안에서_다시_묶인다()
        {
            StageRigRefs rig = BuildRig("stage01");
            StageCutInSlot first = StageFrameBuilder.Build(rig);

            StageRigRefs rebound = _rigBuilder.BindRefsFromRoot(rig.Root, "stage01");
            StageCutInSlot again = StageFrameBuilder.Build(rebound);

            Assert.That(again.Window, Is.SameAs(first.Window), "창을 새로 끼우지 않는다");
            Assert.That(rebound.Camera.Pan, Is.SameAs(rig.Camera.Pan), "창 안의 리그도 그대로 묶인다");
            Assert.That(first.Window.childCount, Is.EqualTo(1), "무대 루트뿐");
        }

        [Test]
        public void 컷인을_열면_창_피벗이_판_아래_가운데로_가고_마스크가_네_점_테두리가_판_아래_외곽선이_된다()
        {
            StageRigRefs rig = BuildRig("stage01");
            StageCutInSlot cutIn = StageFrameBuilder.Build(rig);
            CutInPresetDBSO preset = CutInPresetDBSO.CreateDefault();

            try
            {
                cutIn.SnapOpen(preset, new Vector2(-640f, 50f), 1f, null);

                // 판 아래 가운데 = (−640, 50 − 301/2) → 1920×1080 창 안의 비율.
                Assert.That(cutIn.Window.pivot.x, Is.EqualTo(0.5f - 640f / 1920f).Within(1e-4f));
                Assert.That(cutIn.Window.pivot.y, Is.EqualTo(0.5f + (50f - 150.5f) / 1080f).Within(1e-4f));
                Assert.That(cutIn.Window.rect.size, Is.EqualTo(_mount.rect.size), "피벗을 옮겨도 창 rect는 무대와 같다");
                Assert.That(cutIn.IsOpen, Is.True);
                Assert.That(rig.MaskRoot.GetComponent<StageMaskGraphic>().Kind, Is.EqualTo(StageMaskKind.CustomQuad));

                // 판도 마스크 안이라 아래 외곽선 띠가 같이 잘린다 — 테두리가 판 너비만큼 그 띠를 다시 그린다.
                StageMaskEdgeGraphic edge = rig.MaskEdgeRoot.GetComponent<StageMaskEdgeGraphic>();
                List<StageMaskLineSegment> segments = new();
                rig.MaskRoot.GetComponent<StageMaskGraphic>().CollectEdgeSegments(edge.EdgeMode, segments);
                preset.GetOutlineSegment(new Vector2(-640f, 50f), 1f, out Vector2 outlineA, out Vector2 outlineB);

                Assert.That(edge.enabled, Is.True);
                Assert.That(edge.Thickness, Is.EqualTo(preset.maskInsetPixels).Within(1e-5f));
                Assert.That(edge.color, Is.EqualTo(preset.outlineColor));
                Assert.That(segments.Count, Is.EqualTo(1), "네 점의 변(화면 밖까지)이 아니라 선 하나");
                Assert.That(Vector2.Distance(segments[0].A, outlineA), Is.LessThan(1e-3f));
                Assert.That(Vector2.Distance(segments[0].B, outlineB), Is.LessThan(1e-3f));
                Assert.That(Vector2.Distance(outlineA, outlineB), Is.EqualTo(preset.panelSize.x).Within(2f), "판 너비만큼");

                // 닫힘 — 창은 항등으로 돌아오고 무대는 마스크가 숨긴다(docs/PLAN_stage-visibility.md §3.2).
                cutIn.SnapClose();
                StageMaskGraphic mask = rig.MaskRoot.GetComponent<StageMaskGraphic>();
                Assert.That(cutIn.IsOpen, Is.False);
                Assert.That(cutIn.Window.localScale, Is.EqualTo(Vector3.one), "창을 배율 0으로 남기지 않는다 — 다음 stage_mask가 그 무대를 열면 보여야 한다");
                Assert.That(cutIn.Window.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(edge.enabled, Is.False, "숨은 무대에는 테두리도 없다");
                Assert.That(mask.Kind, Is.EqualTo(StageMaskKind.FullRect), "네 점은 화면 밖까지 뻗어 옮기기만으로는 숨지 않는다 — 무대만 한 사각형으로 바꿔 숨긴다");
                Assert.That(mask.ShapeOffsetPixels, Is.EqualTo(mask.HiddenOffsetPixels));
                Assert.That(Mathf.Abs(mask.HiddenOffsetPixels.x), Is.GreaterThanOrEqualTo(_mount.rect.width), "숨김 자리는 무대 rect 밖이다");

                cutIn.Reset();
                Assert.That(cutIn.Window.localScale, Is.EqualTo(Vector3.one));
                Assert.That(cutIn.Window.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void 길이_0의_흰빛_열림과_흐려져_닫힘은_튀는_모션과_끝_상태가_같다()
        {
            StageRigRefs rig = BuildRig("stage01");
            StageCutInSlot cutIn = StageFrameBuilder.Build(rig);
            CutInPresetDBSO preset = CutInPresetDBSO.CreateDefault();
            StageMaskGraphic mask = rig.MaskRoot.GetComponent<StageMaskGraphic>();

            try
            {
                // 길이를 0으로 — 에디트 모드에서 트윈 없이 끝 상태로 간다.
                preset.flashLineSeconds = 0f;
                preset.flashHoldSeconds = 0f;
                preset.flashClearSeconds = 0f;

                cutIn.PlayFlashOpen(preset, new Vector2(-640f, 50f), 1.4f);

                Assert.That(cutIn.IsOpen, Is.True);
                Assert.That(cutIn.IsPlayingMotion, Is.False);
                Assert.That(cutIn.Window.localScale, Is.EqualTo(Vector3.one), "흰빛 열림은 창을 움직이지 않는다");
                Assert.That(mask.Kind, Is.EqualTo(StageMaskKind.CustomQuad), "다 드러난 카드 — 마스크가 판 모양으로 열렸다");
                Assert.That(cutIn.Window.childCount, Is.EqualTo(1), "길이 0이면 흰 덮개를 세우지도 않는다");

                cutIn.PlayFadeClose(0f);

                Assert.That(cutIn.IsOpen, Is.False);
                Assert.That(cutIn.Window.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(mask.Kind, Is.EqualTo(StageMaskKind.FullRect));
                Assert.That(mask.ShapeOffsetPixels, Is.EqualTo(mask.HiddenOffsetPixels), "튀어 닫힘과 같은 끝 — 마스크 숨김");
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void 창_아래의_흰_덮개_노드는_다시_묶어도_치우지_않고_끈다()
        {
            StageRigRefs rig = BuildRig("stage01");
            StageCutInSlot first = StageFrameBuilder.Build(rig);

            // 흰빛으로 연 적이 있는 무대 — 슬롯이 세운 덮개가 창 아래에 남아 있다(도중에 끊겨 켜진 채로).
            GameObject go = new(StageCutInSlot.FlashNodeName("stage01"), typeof(RectTransform), typeof(CanvasRenderer), typeof(StageCutInFlashGraphic));
            go.transform.SetParent(first.Window, false);
            StageCutInFlashGraphic flash = go.GetComponent<StageCutInFlashGraphic>();
            flash.enabled = true;

            StageCutInSlot rebound = StageFrameBuilder.Build(rig);

            Assert.That(rebound.Window, Is.SameAs(first.Window));
            Assert.That(flash != null, Is.True, "경고 없이 남는다");
            Assert.That(flash.transform.parent, Is.EqualTo(rebound.Window));
            Assert.That(flash.enabled, Is.False, "묶을 때 중립 — 흰빛이 남지 않는다");
        }

        private RectTransform NewChild(RectTransform parent, string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }
    }
}