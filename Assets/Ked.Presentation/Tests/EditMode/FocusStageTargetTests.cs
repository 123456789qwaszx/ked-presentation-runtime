using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ked.Presentation.Core;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// place/size 폴드. 수학은 SettledFocusMath가 이미 성질로 고정했으므로,
    /// 여기서 보는 것은 **배선**이다: 튜닝 조회 → focus 오프셋 합 → 체인 인덱스 → 해.
    ///
    /// 튜닝 값은 실제 덤프(presets/depth.json · focus-tuning.json)의 실측값을 쓴다.
    /// </summary>
    public sealed class FocusStageTargetTests
    {
        private const float Eps = 1e-2f;

        private static Float2Dto F2(float x, float y) => new() { x = x, y = y };
        private static Float3Dto F3(float x, float y, float z) => new() { x = x, y = y, z = z };

        // ── 실측 튜닝 ────────────────────────────────────────────────

        /// <summary>presets/focus-tuning.json의 baseOffsets 실값.</summary>
        private static FocusTuningBodyDto FocusTuning() => new()
        {
            baseOffsets = new FocusOffsetSetDto
            {
                feet = F2(0f, 480f),
                body = F2(0f, 680f),
                bust = F2(0f, 820f),
                face = F2(0f, 950f),
                faceAura = F2(0f, 950f),
                handLeft = F2(-80f, 0f),
                handRight = F2(80f, 0f),
            },
            entries = new List<FocusEntryDto>
            {
                new()
                {
                    key = "bandi",
                    defaultOffset = F2(0f, -20f),
                    offsets = new FocusOffsetSetDto { bust = F2(0f, 10f) },
                },
            },
        };

        /// <summary>presets/depth.json의 level 커브 실값 — 깊이의 유일한 진실.</summary>
        private static DepthLevelTuningDto DepthLevel() => new()
        {
            yCurve = Curve((0f, 120f, 0f, -56f), (20f, -1000f, -56f, 0f)),
            scaleCurve = Curve(
                (0f, 0.86f, 0f, 0.052f),
                (10f, 1.38f, 0.052f, 0.082f),
                (20f, 2.2f, 0.082f, 0f)),
        };

        private static AnimationCurveDto Curve(
            params (float time, float value, float inSlope, float outSlope)[] keys)
        {
            AnimationCurveDto dto = new();

            foreach ((float time, float value, float inSlope, float outSlope) k in keys)
                dto.m_Curve.Add(new KeyframeDto
                {
                    time = k.time, value = k.value, inSlope = k.inSlope, outSlope = k.outSlope,
                });

            return dto;
        }

        // ── 합성 리그 (실제 부모 사슬) ───────────────────────────────

        private static RigSchemaNodeDto Node(string id, string parent, bool bottomPivot = false) => new()
        {
            id = id,
            parent = parent,
            anchoredPosition = F2(0f, 0f),
            anchorMin = F2(0f, 0f),
            anchorMax = F2(1f, 1f),
            pivot = bottomPivot ? F2(0.5f, 0f) : F2(0.5f, 0.5f),
            sizeDelta = F2(0f, 0f),
            localScale = F3(1f, 1f, 1f),
            localEulerAngles = F3(0f, 0f, 0f),
            measuredRectSize = F2(0f, 0f),
        };

        private static StageReducerTuning NewTuning()
        {
            RigSchemaRigDto rig = new()
            {
                rigKind = "character",
                nodes = new List<RigSchemaNodeDto>
                {
                    Node("__root", ""),
                    Node("CharSlot_Track_Focus", "__root"),
                    Node("CharSlot_DepthY", "CharSlot_Track_Focus"),
                    Node("CharSlot_DepthScale", "CharSlot_DepthY", bottomPivot: true),
                    Node("CharSlot_Track_Idle", "CharSlot_DepthScale"),
                    Node("CharSlot_Track", "CharSlot_Track_Idle"),
                    Node("CharSlot_Track_X", "CharSlot_Track"),
                    Node("CharSlot_Track_Y", "CharSlot_Track_X"),
                    Node("CharSlot_Rotation", "CharSlot_Track_Y"),
                    Node("CharSlot_SwayPivot", "CharSlot_Rotation", bottomPivot: true),
                    Node("CharSlot_Scale", "CharSlot_SwayPivot", bottomPivot: true),
                    Node("CharacterPortrait_VisualOffset", "CharSlot_Scale", bottomPivot: true),
                },
            };

            return new StageReducerTuning
            {
                RigSchemas = new RigSchemasFileDto
                {
                    capturedUnderParentSize = F2(1920f, 1080f),
                    rigs = new List<RigSchemaRigDto> { rig },
                },
                ReferenceStageWidth = 1920f,
                BaseResolution = new Vec2(1920f, 1080f),
                RoleAnchors = new RoleAnchorTuningBodyDto(),
                DepthLevel = DepthLevel(),
                FocusTuning = FocusTuning(),
            };
        }

        private static StageCommand Cmd(string name, params string[] args) => new(name, args, "test.yarn:1");

        private static StageState Fold(StageReducerTuning tuning, params StageCommand[] commands)
            => StageReducer.ApplyAll(StageReducer.CreateInitialState(tuning), commands, tuning);

        // 보조 버튼이 하는 일 — 초점이 화면 지점에 오는 자리 축의 끝값을 풀어 그대로 놓는다(대본에는 move_by로 적힌다).
        private static StageState Place(
            StageState state, string slotKey, string focusName, string screenPoint, StageReducerTuning tuning)
        {
            state.TryGetCharacter(slotKey, out string characterKey);

            Assert.That(
                NodeTargets.TryPlaceFocus(
                    state, slotKey, characterKey, focusName, screenPoint, tuning.FocusTuning,
                    out NodeTarget target, out string reason),
                Is.True, reason);

            StageState placed = state.Clone();
            placed.Apply(target);
            return placed;
        }

        // 보조 버튼이 하는 일 — 그 초점이 화면 지점에 오는 줌 · 팬(ShotIntents.FocusTo)을 샷에 놓는다.
        private static StageState FocusTo(
            StageState state, string slotKey, string focusName, string screenPoint, float zoom, StageReducerTuning tuning)
        {
            ScreenPointRatios.TryResolve(state.Nodes.RootSpace.Size, screenPoint, out Vec2 desired);

            StageState focused = state.Clone();

            focused.Shot = ShotIntents.FocusTo(
                state.Shot,
                zoom,
                FocusOf(state, slotKey, focusName, tuning),
                desired,
                state.GetAttachment(slotKey).LayerDepth,
                tuning.ResolveLayerProfileCurve());

            return focused;
        }

        private static Vec2 FocusOf(StageState state, string slotKey, string presetName, StageReducerTuning tuning)
        {
            state.TryGetCharacter(slotKey, out string characterKey);

            Vec2 offset = FocusOffsetMath.Resolve(tuning.FocusTuning, characterKey, presetName, Vec2.Zero);

            return SettledFocusMath.FocusPointInRigSpace(
                state.Nodes.BuildChainTo(StageState.NodeKeyOf(slotKey, "CharacterPortrait_VisualOffset")),
                state.Nodes.RootSpace,
                offset);
        }

        // ── 화면 지점 비율 ───────────────────────────────────────────

        [Test]
        public void 화면_지점은_비율표를_따른다()
        {
            Vec2 frame = new(1920f, 1080f);

            Assert.That(ScreenPointRatios.TryResolve(frame, "center", out Vec2 center), Is.True);
            Assert.That(center, Is.EqualTo(Vec2.Zero));

            ScreenPointRatios.TryResolve(frame, "left", out Vec2 left);
            Assert.That(left.X, Is.EqualTo(-460.8f).Within(Eps), "1920 × 0.24");

            ScreenPointRatios.TryResolve(frame, "tl", out Vec2 tl);
            Assert.That(tl.Y, Is.EqualTo(172.8f).Within(Eps), "1080 × 0.16");

            ScreenPointRatios.TryResolve(frame, "inner_br", out Vec2 innerBr);
            Assert.That(innerBr.X, Is.EqualTo(268.8f).Within(Eps), "1920 × 0.14");
            Assert.That(innerBr.Y, Is.EqualTo(-97.2f).Within(Eps), "1080 × 0.09");
        }

        [Test]
        public void 모르는_화면_지점은_거부한다()
        {
            Assert.That(ScreenPointRatios.TryResolve(new Vec2(1920f, 1080f), "그런곳없음", out _), Is.False);
        }

        // ── focus 오프셋 합 ──────────────────────────────────────────

        [Test]
        public void focus_오프셋은_base와_캐릭터_기본과_프리셋을_더한다()
        {
            // 런타임 ResolveOffset과 같은 합: 820(base bust) - 20(bandi 기본) + 10(bandi bust) = 810
            Vec2 offset = FocusOffsetMath.Resolve(FocusTuning(), "bandi", "bust", Vec2.Zero);

            Assert.That(offset.Y, Is.EqualTo(810f).Within(Eps));
        }

        [Test]
        public void 엔트리가_없는_캐릭터는_base만_적용된다()
        {
            Assert.That(
                FocusOffsetMath.Resolve(FocusTuning(), "모르는캐릭터", "bust", Vec2.Zero).Y,
                Is.EqualTo(820f).Within(Eps));

            Assert.That(
                FocusOffsetMath.Resolve(FocusTuning(), null, "face", Vec2.Zero).Y,
                Is.EqualTo(950f).Within(Eps));
        }

        [Test]
        public void 커맨드_오프셋이_마지막에_더해진다()
        {
            Assert.That(
                FocusOffsetMath.Resolve(FocusTuning(), "bandi", "bust", new Vec2(5f, 100f)).Y,
                Is.EqualTo(910f).Within(Eps));
        }

        // ── 초점을 화면 지점에 (보조 계산) ───────────────────────────

        [Test]
        public void 가운데로_풀면_focus가_원점에_온다()
        {
            StageReducerTuning tuning = NewTuning();

            StageState state = Place(Fold(tuning, Cmd("slot", "c1")), "c1", "bust", "center", tuning);

            Vec2 focus = FocusOf(state, "c1", "bust", tuning);

            Assert.That(focus.X, Is.EqualTo(0f).Within(Eps));
            Assert.That(focus.Y, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void 왼쪽으로_풀면_focus가_화면_왼쪽_지점에_온다()
        {
            StageReducerTuning tuning = NewTuning();

            StageState state = Place(Fold(tuning, Cmd("slot", "c1")), "c1", "bust", "left", tuning);

            Assert.That(FocusOf(state, "c1", "bust", tuning).X, Is.EqualTo(-460.8f).Within(Eps), "1920 × 0.24");
        }

        [Test]
        public void 배역_오프셋을_반영한다()
        {
            // bandi는 bust 오프셋이 810(= 820-20+10)이라 이동량이 달라진다.
            StageReducerTuning tuning = NewTuning();

            StageState plain = Place(Fold(tuning, Cmd("slot", "c1")), "c1", "bust", "center", tuning);
            StageState cast = Place(Fold(tuning, Cmd("slot", "c1"), Cmd("cast", "c1", "bandi")), "c1", "bust", "center", tuning);

            Vec2 plainMove = plain.Nodes.GetState("c1/CharSlot_Track").AnchoredPosition;
            Vec2 castMove = cast.Nodes.GetState("c1/CharSlot_Track").AnchoredPosition;

            Assert.That(castMove.Y - plainMove.Y, Is.EqualTo(10f).Within(Eps),
                "820 → 810이면 10만큼 덜 내려간다");

            // 그래도 명중은 각자의 오프셋 기준으로 성립한다.
            Assert.That(FocusOf(cast, "c1", "bust", tuning).Y, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void 끝값은_move_by가_쓰는_자리_축에_건다()
        {
            // 버튼은 이 끝값과 지금 값의 차를 move_by로 적는다 — 쓰는 명령이 없는 Track_Focus는 건드리지 않는다.
            StageState state = Place(Fold(NewTuning(), Cmd("slot", "c1")), "c1", "bust", "right", NewTuning());

            Assert.That(state.Nodes.GetState("c1/CharSlot_Track").AnchoredPosition, Is.Not.EqualTo(Vec2.Zero));
            Assert.That(state.Nodes.GetState("c1/CharSlot_Track_Focus").AnchoredPosition, Is.EqualTo(Vec2.Zero));
        }

        [Test]
        public void 커지고_기운_뒤에도_명중한다()
        {
            // 자리 축은 깊이 배율 아래에 있다 — 그 위의 배율과 그 아래의 회전까지 넣어 푼다.
            StageReducerTuning tuning = NewTuning();

            StageState before = Fold(tuning,
                Cmd("slot", "c1"),
                Cmd("size", "c1", "close"),
                Cmd("move_by", "c1", "-3u", "1u"),
                Cmd("rotate_by", "c1", "12"));

            StageState state = Place(before, "c1", "face", "tr", tuning);

            ScreenPointRatios.TryResolve(new Vec2(1920f, 1080f), "tr", out Vec2 desired);
            Vec2 focus = FocusOf(state, "c1", "face", tuning);

            Assert.That(focus.X, Is.EqualTo(desired.X).Within(Eps));
            Assert.That(focus.Y, Is.EqualTo(desired.Y).Within(Eps));
        }

        [Test]
        public void 풀린_값을_move_by로_적으면_같은_자리다()
        {
            // 대본에 남는 것은 풀린 값이다 — 끝값과 지금 값의 차를 move_by로 접으면 같은 무대가 나온다.
            StageReducerTuning tuning = NewTuning();

            StageState before = Fold(tuning, Cmd("slot", "c1"), Cmd("size", "c1", "front"), Cmd("move_by", "c1", "2u", "0u"));
            StageState placed = Place(before, "c1", "bust", "left", tuning);

            Vec2 delta = placed.Nodes.GetState("c1/CharSlot_Track").AnchoredPosition
                         - before.Nodes.GetState("c1/CharSlot_Track").AnchoredPosition;

            string x = (delta.X / 40f).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) + "u";
            string y = (delta.Y / 40f).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) + "u";

            StageState written = StageReducer.Apply(before, Cmd("move_by", "c1", x, y), tuning);

            Assert.That(written.Unhandled, Is.Empty);
            Assert.That(FocusOf(written, "c1", "bust", tuning).X, Is.EqualTo(-460.8f).Within(1e-2f));
            Assert.That(FocusOf(written, "c1", "bust", tuning).Y, Is.EqualTo(0f).Within(1e-2f));
        }

        [Test]
        public void 모르는_화면_지점은_풀지_않는다()
        {
            StageReducerTuning tuning = NewTuning();
            StageState state = Fold(tuning, Cmd("slot", "c1"));

            Assert.That(
                NodeTargets.TryPlaceFocus(state, "c1", null, "bust", "그런곳없음", tuning.FocusTuning, out _, out string reason),
                Is.False);

            Assert.That(reason, Does.Contain("화면 지점"));
        }

        [TestCase("place")]
        [TestCase("place_left")]
        [TestCase("place_center")]
        [TestCase("place_right")]
        [TestCase("shot_focus_to")]
        [TestCase("shot_to")]
        [TestCase("show")]
        public void 걷은_역산_묶음_명령은_접지_않는다(string name)
        {
            // 무대를 읽어 값을 푸는 것 · 이름 하나가 여럿을 내는 것은 커맨드가 아니다(docs/PLAN_command-prune.md).
            StageState state = Fold(NewTuning(), Cmd("slot", "c1"), Cmd(name, "c1", "bust"));

            Assert.That(state.Unhandled.Count, Is.EqualTo(1));
            Assert.That(state.Nodes.GetState("c1/CharSlot_Track").AnchoredPosition, Is.EqualTo(Vec2.Zero));
            Assert.That(state.Shot.ToString(), Is.EqualTo(ShotIntentState.Default.ToString()));
        }

        // ── size ─────────────────────────────────────────────────────

        // 라벨은 level 커브 위의 눈금이다(DepthLevelLabels):
        // far=0 · back=10 · mid=14 · front=16 · close=20.
        // scale은 무릎(레벨 10) 아래 0.86+0.052L, 위 1.38+0.082(L-10).
        [TestCase("back", 1.38f)]
        [TestCase("front", 1.872f)]
        [TestCase("close", 2.2f)]
        [TestCase("far", 0.86f)]
        public void size는_레벨의_배율과_Y를_그대로_놓는다(string depthLabel, float expectedScale)
        {
            StageReducerTuning tuning = NewTuning();

            Assert.That(DepthLevelLabels.TryGetLevel(depthLabel, out float level), Is.True);

            StageState before = Fold(tuning, Cmd("slot", "c1"));
            StageState after = StageReducer.Apply(before, Cmd("size", "c1", depthLabel), tuning);

            Assert.That(after.Unhandled.Count, Is.EqualTo(before.Unhandled.Count), "접혀야 한다");

            Assert.That(after.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale.X,
                Is.EqualTo(expectedScale).Within(Eps));

            // Y는 커브의 값 그대로다(120 − 56 × 레벨) — 초점에 맞춰 보정하지 않는다.
            Assert.That(after.Nodes.GetState("c1/CharSlot_DepthY").AnchoredPosition,
                Is.EqualTo(new Vec2(0f, 120f - 56f * level)).Using<Vec2>((a, b) =>
                    System.Math.Abs(a.X - b.X) <= 1e-2f && System.Math.Abs(a.Y - b.Y) <= 1e-2f));
        }

        [Test]
        public void size는_무대를_재지_않는다()
        {
            // 같은 size는 인물이 어디에 어떻게 서 있든 같은 값을 놓는다 — 결과가 인자에 적혀 있다.
            // 그래서 크기가 바뀌면 초점은 움직인다. 제자리에 두려면 자리 명령으로 따로 보정한다.
            StageReducerTuning tuning = NewTuning();

            StageState plain = Fold(tuning, Cmd("slot", "c1"), Cmd("size", "c1", "close"));

            StageState moved = Fold(tuning,
                Cmd("slot", "c1"),
                Cmd("move_by", "c1", "-6u", "2u"),
                Cmd("rotate_by", "c1", "12"),
                Cmd("size", "c1", "close"));

            Assert.That(moved.Unhandled, Is.Empty);

            Assert.That(moved.Nodes.GetState("c1/CharSlot_DepthY").AnchoredPosition,
                Is.EqualTo(plain.Nodes.GetState("c1/CharSlot_DepthY").AnchoredPosition));

            Assert.That(moved.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale,
                Is.EqualTo(plain.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale));

            StageState before = Fold(tuning, Cmd("slot", "c1"));

            Assert.That(
                System.Math.Abs(FocusOf(plain, "c1", "bust", tuning).Y - FocusOf(before, "c1", "bust", tuning).Y),
                Is.GreaterThan(1f),
                "초점을 붙잡지 않는다");
        }

        [Test]
        public void size의_숫자_레벨이_커브로_접힌다()
        {
            // 숫자 레벨: <<size c1 5>>, <<size @4 14>>
            // 무릎(레벨 10) 위쪽도 거부하지 않는다 — 위 구간 기울기 0.082로 선다.
            StageState state = Fold(NewTuning(), Cmd("slot", "c1"), Cmd("size", "c1", "14"));

            Assert.That(state.Unhandled.Count, Is.EqualTo(0));
            Assert.That(state.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale.XY.X,
                Is.EqualTo(1.38f + 0.082f * 4f).Within(1e-3f));
        }

        [Test]
        public void close_위의_무릎이_라벨_구간을_건드리지_않는다()
        {
            // scale 커브는 레벨 10에 무릎이 있다 — 그 위로 더 가파르다(0.052 → 0.082).
            // back(10)이 무릎에 서고 mid·front·close는 무릎 위에 산다.
            StageReducerTuning tuning = NewTuning();

            foreach ((string label, float expected) in new[]
                     {
                         ("far", 0.86f), ("back", 1.38f),
                         ("mid", 1.708f), ("front", 1.872f), ("close", 2.2f),
                     })
            {
                StageState state = Fold(tuning, Cmd("slot", "c1"), Cmd("size", "c1", label));

                Assert.That(state.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale.XY.X,
                    Is.EqualTo(expected).Within(1e-3f), label);
            }

            // 무릎 위: 상한 20이 2.2다(직선 연장이면 1.9였을 자리).
            StageState top = Fold(tuning, Cmd("slot", "c1"), Cmd("size", "c1", "20"));

            Assert.That(top.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale.XY.X,
                Is.EqualTo(2.2f).Within(1e-3f));
        }

        [Test]
        public void 라벨과_같은_레벨의_숫자는_같은_값으로_접힌다()
        {
            // 라벨은 커브 위의 눈금일 뿐이다 — size front와 size 16은 같은 자리다.
            StageReducerTuning tuning = NewTuning();

            StageState byLabel = Fold(tuning, Cmd("slot", "c1"), Cmd("size", "c1", "front"));
            StageState byNumber = Fold(tuning, Cmd("slot", "c1"), Cmd("size", "c1", "16"));

            Assert.That(byNumber.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale.XY.X,
                Is.EqualTo(byLabel.Nodes.GetState("c1/CharSlot_DepthScale").LocalScale.XY.X).Within(1e-4f));

            Assert.That(byNumber.Nodes.GetState("c1/CharSlot_DepthY").AnchoredPosition.Y,
                Is.EqualTo(byLabel.Nodes.GetState("c1/CharSlot_DepthY").AnchoredPosition.Y).Within(1e-3f));
        }

        [Test]
        public void depth_튜닝이_없으면_size가_소리를_낸다()
        {
            StageReducerTuning tuning = NewTuning();
            tuning.DepthLevel = null;

            StageState state = Fold(tuning, Cmd("slot", "c1"), Cmd("size", "c1", "close"));

            Assert.That(state.Unhandled.Any(u => u.Reason.Contains("커브")), Is.True);
        }

        // ── 이징 인자는 폴드에 무해하다 ──────────────────────────────

        [Test]
        public void scale_계열의_마지막_이징_인자는_폴드에_무해하다()
        {
            // 이징은 경로의 모양만 정한다 — 종점(목표 scale)은 그대로다.
            // 지키는 것은 인자 자리다: 이징 토큰이 붙어도 배율을 읽는 인덱스가 밀리면 안 된다.
            StageReducerTuning tuning = NewTuning();

            StageState plain = Fold(tuning,
                Cmd("slot", "c1"), Cmd("scale_by", "c1", "1.5", "24fr"));

            StageState eased = Fold(tuning,
                Cmd("slot", "c1"), Cmd("scale_by", "c1", "1.5", "24fr", "OutBack"));

            Assert.That(eased.Unhandled.Count, Is.EqualTo(0));
            Assert.That(eased.Nodes.GetState("c1/CharSlot_Scale").LocalScale.XY.X,
                Is.EqualTo(plain.Nodes.GetState("c1/CharSlot_Scale").LocalScale.XY.X).Within(Eps));

            // scale_reset도 같다 — 모르는 커브 이름이 붙어도 폴드는 흔들리지 않는다.
            StageState reset = Fold(tuning,
                Cmd("slot", "c1"), Cmd("scale_by", "c1", "1.5"),
                Cmd("scale_reset", "c1", "12fr", "@no_such_curve"));

            Assert.That(reset.Nodes.GetState("c1/CharSlot_Scale").LocalScale.XY.X,
                Is.EqualTo(1f).Within(Eps));
        }

        // ── shot 계열의 이징 인자 ────────────────────────────────────

        [Test]
        public void shot_계열의_마지막_이징_인자는_폴드에_무해하다()
        {
            // 이징은 경로의 모양만 정한다 — 종점(shot intent)은 그대로다.
            // 이 테스트가 지키는 것은 인자 자리다: 이징 토큰이 붙어도
            // zoom·pan을 읽는 인덱스가 밀리지 않아야 한다.
            StageReducerTuning tuning = NewTuning();

            StageState plain = Fold(tuning,
                Cmd("slot", "c1"),
                Cmd("shot_zoom", "3", "24fr"),
                Cmd("shot_pan", "1u", "0u", "24fr"));

            StageState eased = Fold(tuning,
                Cmd("slot", "c1"),
                Cmd("shot_zoom", "3", "24fr", "InOutCubic"),
                Cmd("shot_pan", "1u", "0u", "24fr", "@push_in"));

            Assert.That(eased.Unhandled.Count, Is.EqualTo(0));
            Assert.That(eased.Shot.Zoom, Is.EqualTo(plain.Shot.Zoom).Within(Eps));
            Assert.That(eased.Shot.PanInRigSpace.X, Is.EqualTo(plain.Shot.PanInRigSpace.X).Within(Eps));
            Assert.That(eased.Shot.PanInRigSpace.Y, Is.EqualTo(plain.Shot.PanInRigSpace.Y).Within(Eps));
        }

        // ── 초점을 화면 지점에 오게 하는 줌 · 팬 (보조 계산) ─────────

        [Test]
        public void 줌과_팬을_풀면_적용측_규약으로_화면_지점에_온다()
        {
            // 명중 검산: slot 생략값은 mid라 층 반응이 항등이다 — 보이는 위치 = 논리 focus × 목표 배율 + pan.
            StageReducerTuning tuning = NewTuning();

            StageState placed = Place(Fold(tuning, Cmd("slot", "c1")), "c1", "bust", "center", tuning);
            StageState state = FocusTo(placed, "c1", "face", "center", 1.6f, tuning);

            Assert.That(state.Shot.Zoom, Is.EqualTo(1.6f).Within(Eps));

            float scale = ShotIntentMath.EvaluateCameraScale(state.Shot.Zoom);

            Vec2 visible = new(
                state.Shot.FocusPointInRigSpace.X * scale + state.Shot.PanInRigSpace.X,
                state.Shot.FocusPointInRigSpace.Y * scale + state.Shot.PanInRigSpace.Y);

            Assert.That(visible.X, Is.EqualTo(0f).Within(Eps), "center에 명중");
            Assert.That(visible.Y, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void 줌과_팬은_지금_카메라와_무관하게_무대_위_자리로_푼다()
        {
            StageReducerTuning tuning = NewTuning();

            // 카메라가 이미 움직인 상태에서 풀어도 명중한다 — 입력은 무대 좌표다.
            StageState moved = Fold(tuning,
                Cmd("slot", "c1"),
                Cmd("shot_zoom", "3"),
                Cmd("shot_pan", "1u", "0u"));

            StageState state = FocusTo(moved, "c1", "bust", "left", 2f, tuning);

            float scale = ShotIntentMath.EvaluateCameraScale(2f);

            Vec2 visible = new(
                state.Shot.FocusPointInRigSpace.X * scale + state.Shot.PanInRigSpace.X,
                state.Shot.FocusPointInRigSpace.Y * scale + state.Shot.PanInRigSpace.Y);

            ScreenPointRatios.TryResolve(new Vec2(1920f, 1080f), "left", out Vec2 desired);

            Assert.That(visible.X, Is.EqualTo(desired.X).Within(Eps));
            Assert.That(visible.Y, Is.EqualTo(desired.Y).Within(Eps));
        }

        [TestCase("far")]
        [TestCase("back")]
        [TestCase("front")]
        [TestCase("close")]
        public void 줌과_팬은_슬롯이_선_층의_반응까지_넣어_명중한다(string layer)
        {
            // 층 반응이 항등이 아닌 층. 정방향 식(SolveLayer → ProjectToRigSpace)으로 보이는 자리를 검산한다.
            StageReducerTuning tuning = NewTuning();

            StageState placed = Place(Fold(tuning, Cmd("slot", "c1", "stage00", layer)), "c1", "bust", "left", tuning);
            StageState state = FocusTo(placed, "c1", "bust", "center", 2.5f, tuning);

            float depth = state.GetAttachment("c1").LayerDepth;
            Vec2 focus = FocusOf(state, "c1", "bust", tuning);
            Assert.That(state.Shot.FocusPointInRigSpace.X, Is.EqualTo(focus.X).Within(Eps), "논리 focus = 무대 위 자리");
            Assert.That(state.Shot.FocusPointInRigSpace.Y, Is.EqualTo(focus.Y).Within(Eps), "논리 focus = 무대 위 자리");

            CameraPose camera = ShotLayoutMath.SolveCamera(state.Shot);
            LayerResponse response = ShotLayoutMath.SolveLayer(state.Shot, depth, LayerProfileCurve.Default);
            Vec2 seen = ShotLayoutMath.ProjectToRigSpace(focus, in camera, in response);

            Assert.That(seen.X, Is.EqualTo(0f).Within(Eps), $"{layer} center에 명중 — seen={seen}");
            Assert.That(seen.Y, Is.EqualTo(0f).Within(Eps), $"{layer} center에 명중 — seen={seen}");
        }
    }
}
