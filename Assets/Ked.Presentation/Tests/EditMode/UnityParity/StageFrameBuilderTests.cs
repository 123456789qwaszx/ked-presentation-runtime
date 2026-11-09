using System.Collections.Generic;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests.UnityParity
{
    /// <summary>
    /// 무대 틀(창 · 컷인 판 · 그림 팝업)은 무대 루트 바깥에서 리그를 감싼다 — StageFrameBuilder.
    /// 리그만 세운 계층은 StageRigBuilderTests.
    /// </summary>
    public sealed class StageFrameBuilderTests
    {
        private readonly List<GameObject> _spawned = new();
        private RectTransform _mount;
        private StageRigBuilder _rigBuilder;
        private StageFrameBuilder _frameBuilder;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new("__Mount", typeof(RectTransform));
            _spawned.Add(go);
            _mount = (RectTransform)go.transform;
            _mount.sizeDelta = new Vector2(1920f, 1080f);
            _rigBuilder = new StageRigBuilder();
            _frameBuilder = new StageFrameBuilder();
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
        public void 창이_무대_루트를_감싸고_판이_무대보다_뒤다()
        {
            StageRigRefs rig = BuildRig("stage00");
            _frameBuilder.Build(rig, out StageCutInSlot cutIn, out _);

            Assert.That(rig.Root.parent, Is.EqualTo(cutIn.Window), "무대 루트는 창 아래");
            Assert.That(cutIn.Window.parent, Is.EqualTo(_mount));
            Assert.That(cutIn.Window.name, Is.EqualTo("stage00_Window"));
            Assert.That(cutIn.Panel.parent, Is.EqualTo(cutIn.Window));
            Assert.That(cutIn.Panel.GetSiblingIndex(), Is.EqualTo(0), "판이 무대보다 뒤");
            Assert.That(rig.Root.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(cutIn.PanelImage.enabled, Is.False, "판은 닫힌 채 선다");
        }

        [Test]
        public void 옛_씬의_무대_루트는_부수지_않고_창으로_감싼다()
        {
            // 틀 이전 계층: mount 바로 아래 무대 루트. 다른 무대 둘 사이의 그리기 순서를 지킨다.
            RectTransform before = NewChild(_mount, "Before");
            StageRigRefs rig = BuildRig("stage01");
            RectTransform after = NewChild(_mount, "After");

            RectTransform pan = rig.CameraPan;
            _frameBuilder.Build(rig, out StageCutInSlot cutIn, out _);

            Assert.That(rig.CameraPan, Is.SameAs(pan), "무대 계층은 그대로");
            Assert.That(rig.Root.parent, Is.EqualTo(cutIn.Window));
            Assert.That(cutIn.Window.parent, Is.EqualTo(_mount));
            Assert.That(before.GetSiblingIndex(), Is.LessThan(cutIn.Window.GetSiblingIndex()));
            Assert.That(after.GetSiblingIndex(), Is.GreaterThan(cutIn.Window.GetSiblingIndex()));
        }

        [Test]
        public void 다시_묶으면_있는_창을_쓰고_리그도_창_안에서_다시_묶인다()
        {
            StageRigRefs rig = BuildRig("stage01");
            _frameBuilder.Build(rig, out StageCutInSlot first, out _);

            StageRigRefs rebound = _rigBuilder.BindRefsFromRoot(rig.Root, "stage01");
            _frameBuilder.Build(rebound, out StageCutInSlot again, out _);

            Assert.That(again.Window, Is.SameAs(first.Window), "창을 새로 끼우지 않는다");
            Assert.That(rebound.CameraPan, Is.SameAs(rig.CameraPan), "창 안의 리그도 그대로 묶인다");
            Assert.That(first.Window.childCount, Is.EqualTo(2), "판과 무대 루트뿐");
        }

        [Test]
        public void 컷인을_열면_창_피벗이_판_아래_가운데로_가고_마스크가_네_점이_된다()
        {
            StageRigRefs rig = BuildRig("stage01");
            _frameBuilder.Build(rig, out StageCutInSlot cutIn, out _);
            CutInPresetDBSO preset = CutInPresetDBSO.CreateDefault();

            try
            {
                cutIn.SnapOpen(preset, null, new Vector2(-640f, 50f), 1f, null);

                // 판 아래 가운데 = (−640, 50 − 301/2) → 1920×1080 창 안의 비율.
                Assert.That(cutIn.Window.pivot.x, Is.EqualTo(0.5f - 640f / 1920f).Within(1e-4f));
                Assert.That(cutIn.Window.pivot.y, Is.EqualTo(0.5f + (50f - 150.5f) / 1080f).Within(1e-4f));
                Assert.That(cutIn.Window.rect.size, Is.EqualTo(_mount.rect.size), "피벗을 옮겨도 창 rect는 무대와 같다");
                Assert.That(cutIn.Panel.sizeDelta, Is.EqualTo(preset.panelSize));
                Assert.That(cutIn.IsOpen, Is.True);
                Assert.That(rig.MaskRoot.GetComponent<StageMaskGraphic>().Kind, Is.EqualTo(StageMaskKind.CustomQuad));

                cutIn.SnapClose();
                Assert.That(cutIn.Window.localScale, Is.EqualTo(new Vector3(0f, 0f, 1f)));

                cutIn.Reset();
                Assert.That(cutIn.Window.localScale, Is.EqualTo(Vector3.one));
                Assert.That(cutIn.Window.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(cutIn.PanelImage.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }

        // ── 그림 팝업 (docs/PLAN_popup.md) ──────────────────────────

        [Test]
        public void 팝업은_창의_다음_형제이고_뒷막이_그림보다_뒤다()
        {
            _frameBuilder.Build(BuildRig("stage01"), out StageCutInSlot c1, out StagePopupSlot p1);
            _frameBuilder.Build(BuildRig("stage02"), out StageCutInSlot c2, out _);

            Assert.That(p1.Group.name, Is.EqualTo("stage01_Popup"));
            Assert.That(p1.Group.parent, Is.EqualTo(_mount));
            Assert.That(p1.Group.GetSiblingIndex(), Is.EqualTo(c1.Window.GetSiblingIndex() + 1), "그 무대 내용 바로 위");
            Assert.That(p1.Group.GetSiblingIndex(), Is.LessThan(c2.Window.GetSiblingIndex()), "다음 무대 아래");
            Assert.That(p1.Backdrop.transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(p1.Picture.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(p1.Group.childCount, Is.EqualTo(2));
            Assert.That(p1.Canvas.blocksRaycasts, Is.False);

            // 닫힌 채 선다.
            Assert.That(p1.IsOpen, Is.False);
            Assert.That(p1.Alpha, Is.EqualTo(0f));
            Assert.That(p1.Backdrop.enabled, Is.False);
            Assert.That(p1.PictureImage.enabled, Is.False);
        }

        [Test]
        public void 다시_묶어도_팝업은_창_다음_자리로_돌아온다()
        {
            StageRigRefs s1 = BuildRig("stage01");
            _frameBuilder.Build(s1, out _, out StagePopupSlot p1);
            _frameBuilder.Build(BuildRig("stage02"), out _, out _);

            RectTransform group = p1.Group;
            group.SetAsFirstSibling();   // 엉뚱한 자리(창 앞)

            _frameBuilder.Build(s1, out StageCutInSlot cutIn, out StagePopupSlot rebound);

            Assert.That(rebound.Group, Is.SameAs(group), "부수지 않고 다시 묶는다");
            Assert.That(group.GetSiblingIndex(), Is.EqualTo(cutIn.Window.GetSiblingIndex() + 1));
        }

        [Test]
        public void 팝업을_열면_그림이_원본_픽셀_곱하기_배율로_서고_닫으면_꺼진다()
        {
            _frameBuilder.Build(BuildRig("stage01"), out _, out StagePopupSlot popup);
            PopupPresetDBSO preset = PopupPresetDBSO.CreateDefault();
            Texture2D tex = new(404, 404);
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 404f, 404f), new Vector2(0.5f, 0.5f), 100f);

            try
            {
                popup.SnapOpen(preset, sprite, new Vector2(40f, -20f), 1.5f);

                Assert.That(popup.IsOpen, Is.True);
                Assert.That(popup.Alpha, Is.EqualTo(1f));
                Assert.That(popup.Picture.sizeDelta.x, Is.EqualTo(404f * 0.91f * 1.5f).Within(1e-3f));
                Assert.That(popup.Picture.anchoredPosition, Is.EqualTo(new Vector2(40f, -20f)));
                Assert.That(popup.Backdrop.enabled && popup.PictureImage.enabled, Is.True);
                Assert.That(popup.Backdrop.color.a, Is.EqualTo(0.47f).Within(1e-4f));

                popup.SnapClose();
                Assert.That(popup.IsOpen, Is.False);
                Assert.That(popup.Alpha, Is.EqualTo(0f));
                Assert.That(popup.PictureImage.enabled, Is.False);

                popup.SnapOpen(preset, null, Vector2.zero, 1f);
                Assert.That(popup.PictureImage.enabled, Is.False, "그림이 없으면 뒷막만");
                Assert.That(popup.Backdrop.enabled, Is.True);

                popup.Reset();
                Assert.That(popup.IsOpen, Is.False);
                Assert.That(popup.Backdrop.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(preset);
            }
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
