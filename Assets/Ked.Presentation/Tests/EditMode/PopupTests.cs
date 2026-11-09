using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 그림 팝업 — 폴드의 끝 상태(docs/PLAN_popup.md §7)와 프리셋 페이드 기본값(§4, 원본 실측).
    /// 계층 · 크기는 UnityParity/StageRigBuilderTests, 문법은 StageCommandParserTests.
    /// </summary>
    public sealed class PopupTests
    {
        private static StageReducerTuning NewTuning()
            => new()
            {
                BaseResolution = new Vec2(1920f, 1080f),
                ReferenceStageWidth = 1920f,
            };

        private static StageCommand Cmd(string name, params string[] args) => new(name, args, "test.yarn:1");

        private static StageState Fold(params StageCommand[] commands)
        {
            StageReducerTuning tuning = NewTuning();
            return StageReducer.ApplyAll(StageReducer.CreateInitialState(tuning), commands, tuning);
        }

        // ── 폴드 ─────────────────────────────────────────────────────

        [Test]
        public void 열면_무대에_열린_팝업이_남는다()
        {
            StageState state = Fold(Cmd("popup", "stage01", "sample_letter", "2u", "-1u", "1.2"));

            Assert.That(state.TryGetPopup("stage01", out PopupState p), Is.True);
            Assert.That(p.IsOpen, Is.True);
            Assert.That(p.PictureKey, Is.EqualTo("sample_letter"));
            Assert.That(p.Center.X, Is.EqualTo(80f).Within(1e-3f));
            Assert.That(p.Center.Y, Is.EqualTo(-40f).Within(1e-3f));
            Assert.That(p.Scale, Is.EqualTo(1.2f).Within(1e-6f));
            Assert.That(state.Unhandled, Is.Empty);
        }

        [Test]
        public void 닫으면_자리는_남고_닫힘이_된다()
        {
            StageState state = Fold(Cmd("popup", "stage02", "sample_seedling"), Cmd("popup_close", "stage02"));

            Assert.That(state.TryGetPopup("stage02", out PopupState p), Is.True);
            Assert.That(p.IsOpen, Is.False);
            Assert.That(p.PictureKey, Is.EqualTo("sample_seedling"));
            Assert.That(state.Unhandled, Is.Empty);
        }

        [Test]
        public void 열린_팝업에_다시_쓰면_그림만_바뀐다()
        {
            StageState state = Fold(Cmd("popup", "stage01", "sample_seedling"), Cmd("popup", "stage01", "sample_letter"));

            Assert.That(state.TryGetPopup("stage01", out PopupState p) && p.IsOpen, Is.True);
            Assert.That(p.PictureKey, Is.EqualTo("sample_letter"));
            Assert.That(state.Unhandled, Is.Empty);
        }

        [Test]
        public void 무대를_생략하면_stage01이다()
        {
            StageState state = Fold(Cmd("popup", "", "sample_letter"));
            Assert.That(state.TryGetPopup("stage01", out PopupState p) && p.IsOpen, Is.True);
        }

        [Test]
        public void 본_무대에는_열_수_없다()
        {
            StageState state = Fold(Cmd("popup", "stage00", "sample_letter"));

            Assert.That(state.Popups, Is.Empty);
            Assert.That(state.Unhandled.Count, Is.EqualTo(1));
            Assert.That(state.Unhandled[0].Reason, Does.Contain("본 무대"));
        }

        [Test]
        public void 열지_않은_무대를_닫으면_Unhandled다()
        {
            StageState state = Fold(Cmd("popup_close", "stage02"));

            Assert.That(state.Unhandled.Count, Is.EqualTo(1));
            Assert.That(state.Unhandled[0].Reason, Does.Contain("열린 그림 팝업이 없다"));
        }

        [Test]
        public void 그림이_없거나_배율이_0이면_Unhandled다()
        {
            Assert.That(Fold(Cmd("popup", "stage01")).Unhandled.Count, Is.EqualTo(1));
            Assert.That(Fold(Cmd("popup", "stage01", "sample_letter", "0u", "0u", "0")).Unhandled.Count, Is.EqualTo(1));
        }

        [Test]
        public void 팝업은_노드를_만들지_않고_컷인과_따로_접힌다()
        {
            StageState state = Fold(
                Cmd("cutin_open", "stage01", "pink_stars"),
                Cmd("popup", "stage01", "sample_letter"),
                Cmd("cutin_close", "stage01"));

            Assert.That(state.Nodes.Count, Is.EqualTo(0), "팝업은 무대 루트 바깥 — 리그 노드 트리에 들지 않는다");
            Assert.That(state.TryGetPopup("stage01", out PopupState p) && p.IsOpen, Is.True, "컷인을 닫아도 팝업은 그대로");
            Assert.That(state.TryGetCutIn("stage01", out CutInState c) && !c.IsOpen, Is.True);
        }

        // ── 프리셋 ───────────────────────────────────────────────────

        [Test]
        public void 기본_페이드는_규칙을_지키고_실측_점을_지난다()
        {
            PopupPresetDBSO preset = PopupPresetDBSO.CreateDefault();

            try
            {
                CurveKey[] open = preset.OpenKeys(out string a);
                CurveKey[] close = preset.CloseKeys(out string b);

                Assert.That(open, Is.Not.Null, a);
                Assert.That(close, Is.Not.Null, b);

                // 등장 약 0.4초, 퇴장 약 0.3초(docs/PLAN_popup.md §1).
                Assert.That(open[open.Length - 1].Time, Is.EqualTo(0.445f).Within(1e-6f));
                Assert.That(close[close.Length - 1].Time, Is.EqualTo(0.305f).Within(1e-6f));

                Assert.That(CurveFunctions.Evaluate(open, 0f), Is.EqualTo(0f));
                Assert.That(CurveFunctions.Evaluate(open, 0.245f), Is.EqualTo(0.75f).Within(1e-4f));
                Assert.That(CurveFunctions.Evaluate(open, 1f), Is.EqualTo(1f));
                Assert.That(CurveFunctions.Evaluate(close, 0.165f), Is.EqualTo(0.51f).Within(1e-4f));
                Assert.That(CurveFunctions.Evaluate(close, 1f), Is.EqualTo(0f));

                // 넘치지 않는다 — 불투명도가 0~1 밖으로 나가지 않는다(커맨드는 그래도 자른다).
                for (float t = 0f; t <= 0.45f; t += 0.005f)
                {
                    float v = CurveFunctions.Evaluate(open, t);
                    Assert.That(v, Is.InRange(-1e-3f, 1f + 1e-3f), $"등장 t={t}");
                }

                Assert.That(preset.pictureScale, Is.EqualTo(0.91f));
                Assert.That(preset.backdropColor.a, Is.EqualTo(0.47f).Within(1e-6f));
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void 어긋난_페이드는_null과_이유를_낸다()
        {
            PopupPresetDBSO preset = PopupPresetDBSO.CreateDefault();

            try
            {
                preset.openTimes = new[] { 0f, 0.2f, 0.1f };
                preset.openAlpha = new[] { 0f, 0.5f, 1f };
                Assert.That(preset.OpenKeys(out string why), Is.Null);
                Assert.That(why, Does.Contain("오름차순"));

                preset.closeTimes = new[] { 0f, 0.3f };
                preset.closeAlpha = new[] { 1f, 0.2f };
                Assert.That(preset.CloseKeys(out why), Is.Null);
                Assert.That(why, Does.Contain("끝나야"));
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }
    }
}
