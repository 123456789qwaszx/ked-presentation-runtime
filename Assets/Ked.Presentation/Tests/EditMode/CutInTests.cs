using System.Collections.Generic;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 컷인 — 폴드의 끝 상태(docs/PLAN_cutin.md §6)와 프리셋의 모양 · 모션 기본값.
    /// </summary>
    public sealed class CutInTests
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
        public void 열면_무대에_열린_컷인이_남는다()
        {
            StageState state = Fold(Cmd("cutin_open", "stage01", "pink_stars", "-16u", "1.25u", "0.86"));

            Assert.That(state.TryGetCutIn("stage01", out CutInState c), Is.True);
            Assert.That(c.IsOpen, Is.True);
            Assert.That(c.PanelKey, Is.EqualTo("pink_stars"));
            Assert.That(c.Center.X, Is.EqualTo(-640f).Within(1e-3f));
            Assert.That(c.Center.Y, Is.EqualTo(50f).Within(1e-3f));
            Assert.That(c.Scale, Is.EqualTo(0.86f).Within(1e-6f));
            Assert.That(state.Unhandled, Is.Empty);
        }

        [Test]
        public void 폴드는_여닫는_모션을_모른다_끝_상태가_같다()
        {
            StageState pop = Fold(
                Cmd("cutin_open", "stage01", "pink_stars", "-16u", "1.25u", "1.3"),
                Cmd("cutin_close", "stage01"));

            StageState flash = Fold(
                Cmd("cutin_open", "stage01", "pink_stars", "-16u", "1.25u", "1.3", "cutin_flash_in"),
                Cmd("cutin_close", "stage01", "cutin_fade_out"));

            Assert.That(flash.Unhandled.Count, Is.EqualTo(0), "모션 인자는 폴드를 막지 않는다");
            Assert.That(flash.TryGetCutIn("stage01", out CutInState a), Is.True);
            Assert.That(pop.TryGetCutIn("stage01", out CutInState b), Is.True);
            Assert.That(a.ToString(), Is.EqualTo(b.ToString()));
            Assert.That(a.IsOpen, Is.False);
        }

        [Test]
        public void 닫으면_자리는_남고_닫힘이_된다()
        {
            StageState state = Fold(
                Cmd("cutin_open", "stage01", "blue_question", "10u", "0u"),
                Cmd("cutin_close", "stage01"));

            Assert.That(state.TryGetCutIn("stage01", out CutInState c), Is.True);
            Assert.That(c.IsOpen, Is.False);
            Assert.That(c.PanelKey, Is.EqualTo("blue_question"));
            Assert.That(state.Unhandled, Is.Empty);
        }

        [Test]
        public void 무대를_생략하면_stage01이다()
        {
            StageState state = Fold(Cmd("cutin_open", "", "pink_stars"));
            Assert.That(state.TryGetCutIn("stage01", out CutInState c) && c.IsOpen, Is.True);
        }

        [Test]
        public void 본_무대에는_열_수_없다()
        {
            StageState state = Fold(Cmd("cutin_open", "stage00", "pink_stars"));

            Assert.That(state.CutIns, Is.Empty);
            Assert.That(state.Unhandled.Count, Is.EqualTo(1));
            Assert.That(state.Unhandled[0].Reason, Does.Contain("본 무대"));
        }

        [Test]
        public void 열지_않은_무대를_닫으면_Unhandled다()
        {
            StageState state = Fold(Cmd("cutin_close", "stage02"));

            Assert.That(state.Unhandled.Count, Is.EqualTo(1));
            Assert.That(state.Unhandled[0].Reason, Does.Contain("열린 컷인이 없다"));
        }

        [Test]
        public void 판이_없거나_배율이_0이면_Unhandled다()
        {
            Assert.That(Fold(Cmd("cutin_open", "stage01")).Unhandled.Count, Is.EqualTo(1));
            Assert.That(Fold(Cmd("cutin_open", "stage01", "pink_stars", "0u", "0u", "0")).Unhandled.Count, Is.EqualTo(1));
        }

        [Test]
        public void 컷인은_노드를_만들지_않는다()
        {
            // 창은 무대 루트 바깥이다 — 리그 노드 트리(리그 로컬 값)에 들지 않는다.
            StageState state = Fold(Cmd("cutin_open", "stage01", "pink_stars", "-16u", "1u"), Cmd("cutin_close", "stage01"));
            Assert.That(state.Nodes.Count, Is.EqualTo(0));
        }

        // ── 프리셋 ───────────────────────────────────────────────────

        [Test]
        public void 기본_모션은_규칙을_지키고_끝이_열림과_닫힘이다()
        {
            MotionClip popIn = CutInPresetDBSO.DefaultPopIn();
            MotionClip popOut = CutInPresetDBSO.DefaultPopOut();

            Assert.That(MotionRules.TryValidate(popIn, null, out string a), Is.True, a);
            Assert.That(MotionRules.TryValidate(popOut, null, out string b), Is.True, b);

            Vec3 one = new(1f, 1f, 1f);
            Assert.That(MotionMath.EndValue(popIn.Channels[0], MotionChannelMode.Relative, one).X, Is.EqualTo(1f).Within(1e-6f));
            Assert.That(MotionMath.EndValue(popIn.Channels[1], MotionChannelMode.Relative, default).Z, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(MotionMath.EndValue(popOut.Channels[0], MotionChannelMode.Relative, one).X, Is.EqualTo(0f).Within(1e-6f));

            // 실측 지점을 지난다 — 등장 +140ms 1.19배.
            Assert.That(MotionMath.Sample(popIn.Channels[0], 0.14f).X, Is.EqualTo(1.19f).Within(1e-4f));
        }

        // ── 흰빛 열림(cutin_flash_in) ────────────────────────────────

        [Test]
        public void 흰빛_열림은_선_판_머묾_걷힘의_차례다()
        {
            CutInPresetDBSO preset = CutInPresetDBSO.CreateDefault();

            try
            {
                Assert.That(preset.FlashDuration, Is.EqualTo(0.15f + 0.08f + 0.3f).Within(1e-5f));

                // ① 선 — 자라는 중, 아직 판이 아니다.
                CutInFlashFrame start = preset.EvaluateFlash(0f);
                Assert.That(start.IsPlate, Is.False);
                Assert.That(start.LineProgress, Is.EqualTo(0f).Within(1e-6f));

                CutInFlashFrame line = preset.EvaluateFlash(0.075f);
                Assert.That(line.IsPlate, Is.False);
                Assert.That(line.LineProgress, Is.EqualTo(0.5f).Within(1e-4f));
                Assert.That(line.Whiteness, Is.EqualTo(1f));

                // ② · ③ 판 — 선이 끝난 순간부터 온통 흰 판으로 머문다.
                Assert.That(preset.EvaluateFlash(0.15f).IsPlate, Is.True);
                Assert.That(preset.EvaluateFlash(0.15f).Whiteness, Is.EqualTo(1f));
                Assert.That(preset.EvaluateFlash(0.22f).Whiteness, Is.EqualTo(1f), "머묾 동안은 온통 희다");

                // ④ 걷힘 — 1 → 0으로 고르게.
                Assert.That(preset.EvaluateFlash(0.23f + 0.15f).Whiteness, Is.EqualTo(0.5f).Within(1e-3f));
                Assert.That(preset.EvaluateFlash(0.23f + 0.3f).Whiteness, Is.EqualTo(0f).Within(1e-4f));
                Assert.That(preset.EvaluateFlash(10f).Whiteness, Is.EqualTo(0f));

                // 30fps로 찍으면: 온통 흰 프레임 2 ~ 3장 뒤, 8 ~ 10장에 걸쳐 내려간다(작업지시서의 기준).
                int white = 0, clearing = 0;
                for (int frame = 0; frame < 60; frame++)
                {
                    CutInFlashFrame f = preset.EvaluateFlash(frame / 30f);
                    if (!f.IsPlate)
                        continue;

                    if (f.Whiteness >= 1f)
                        white++;
                    else if (f.Whiteness > 0f)
                        clearing++;
                }

                Assert.That(white, Is.InRange(2, 3));
                Assert.That(clearing, Is.InRange(8, 10));
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void 흰_선은_판_가운데_높이를_판의_기울기대로_왼쪽부터_긋는다()
        {
            CutInPresetDBSO preset = CutInPresetDBSO.CreateDefault();

            try
            {
                Vector2 center = new(-640f, 50f);
                preset.GetCorners(center, 1.4f, out Vector2 bl, out Vector2 tl, out Vector2 tr, out Vector2 br);

                Vector2 left = (bl + tl) * 0.5f;
                Vector2 right = (br + tr) * 0.5f;

                preset.GetFlashLineQuad(center, 1.4f, 1f, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3);

                Assert.That(Vector2.Distance((p0 + p1) * 0.5f, left), Is.LessThan(1e-3f), "왼쪽 변 가운데에서 시작");
                Assert.That(Vector2.Distance((p2 + p3) * 0.5f, right), Is.LessThan(1e-3f), "다 자라면 오른쪽 변 가운데까지");
                Assert.That(Vector2.Distance(p0, p1), Is.EqualTo(preset.flashLineThickness * 1.4f).Within(1e-3f), "굵기에 배율이 곱해진다");

                preset.GetFlashLineQuad(center, 1.4f, 0.5f, out _, out _, out Vector2 h2, out Vector2 h3);
                Assert.That(Vector2.Distance((h2 + h3) * 0.5f, (left + right) * 0.5f), Is.LessThan(1e-3f), "반쯤 자라면 가운데까지");
                Assert.That(Vector2.Distance(h2, h3), Is.LessThan(Vector2.Distance(p2, p3)), "자라며 굵어진다");
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void 에셋에서_읽은_클립의_빈_성분은_성분_없음이다()
        {
            // 유니티 직렬화는 null 배열을 빈 배열로 저장한다 — 배율 채널의 Z가 빈 배열로 돌아온다(2026-11-09 실제 경고).
            MotionClip serialized = CutInPresetDBSO.DefaultPopIn();
            serialized.Channels[0].Z = new CurveKey[0];
            serialized.Channels[1].X = new CurveKey[0];
            serialized.Channels[1].Y = new CurveKey[0];

            Assert.That(MotionRules.TryValidate(serialized, null, out _), Is.False, "그대로면 규칙에 걸린다");
            Assert.That(MotionRules.TryValidate(CutInPresetDBSO.ReadClip(serialized), null, out string why), Is.True, why);
        }

        [Test]
        public void 마스크는_판_아래_변으로_자르고_위로_연다()
        {
            CutInPresetDBSO preset = CutInPresetDBSO.CreateDefault();

            try
            {
                preset.GetCorners(Vector2.zero, 1f, out Vector2 bl, out _, out _, out Vector2 br);
                preset.GetMaskQuad(Vector2.zero, 1f, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3);

                // 아래 변(안쪽으로 maskInset만큼)의 두 점이 p0–p3 선 위에 있다.
                Vector2 along = (p3 - p0).normalized;
                Vector2 up = new(-along.y, along.x);
                Assert.That(Vector2.Dot(bl - p0, up), Is.EqualTo(-preset.maskInsetPixels).Within(1e-3f));
                Assert.That(Vector2.Dot(br - p0, up), Is.EqualTo(-preset.maskInsetPixels).Within(1e-3f));

                // 위로 화면 밖까지 연다.
                Assert.That(p1.y, Is.GreaterThan(1080f));
                Assert.That(p2.y, Is.GreaterThan(1080f));
                Assert.That(p0.x, Is.LessThan(-1920f), "옆은 자르지 않는다(기본)");
                Assert.That(p3.x, Is.GreaterThan(1920f));
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }
    }
}
