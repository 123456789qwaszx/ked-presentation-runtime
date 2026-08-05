using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Ked.Presentation.Core.Tests
{
    /// <summary>
    /// U14-c2 골든: place/size 폴드. 성질로 고정한다 —
    /// place: 접은 뒤 focus가 화면 지점에 선다. size: 접은 뒤 focus가 제자리다.
    /// 값은 실제 덤프 모양(depth mid=1.0/close=1.38, focus bust=820 등)을 쓴다.
    /// </summary>
    public sealed class FocusStageReductionTests
    {
        private const float Eps = 1e-2f;

        private static Float2Dto F2(float x, float y) => new Float2Dto { x = x, y = y };

        private static DepthPresetSetDto MakeDepthPresets() => new DepthPresetSetDto
        {
            mid = new DepthPresetDto
            {
                depthY = F2(0f, 0f), depthScale = 1f,
                preserveFocusPreset = 20, preserveFocusOffset = F2(0f, 0f),
            },
            close = new DepthPresetDto
            {
                depthY = F2(0f, 440f), depthScale = 1.38f,
                preserveFocusPreset = 30, preserveFocusOffset = F2(0f, 0f),
            },
            back = new DepthPresetDto
            {
                depthY = F2(0f, 240f), depthScale = 1.14f,
                preserveFocusPreset = 20, preserveFocusOffset = F2(0f, 0f),
            },
        };

        private static FocusTuningBodyDto MakeFocusTuning() => new FocusTuningBodyDto
        {
            baseOffsets = new FocusOffsetSetDto
            {
                feet = F2(0f, 480f), body = F2(0f, 680f),
                bust = F2(0f, 820f), face = F2(0f, 950f),
            },
            entries =
            {
                new FocusEntryDto
                {
                    key = "parkeunseol",
                    defaultOffset = F2(0f, -20f),
                    offsets = new FocusOffsetSetDto { bust = F2(0f, 10f) },
                },
            },
        };

        private static StageReducerTuning MakeTuning()
        {
            // StageReducerTests의 미니 스키마를 재사용하기 위해 같은 모양으로 만든다.
            Float3Dto F3(float x, float y, float z) => new Float3Dto { x = x, y = y, z = z };

            string[] chain =
            {
                "__root", "CharSlot_Track_Focus", "CharSlot_DepthY", "CharSlot_DepthScale",
                "CharSlot_Track", "CharSlot_Track_X", "CharSlot_Track_Y",
                "CharSlot_Rotation", "CharSlot_SwayPivot", "CharSlot_Scale",
                "CharacterPortrait_VisualOffset",
            };

            List<RigSchemaNodeDto> nodes = new();

            for (int i = 0; i < chain.Length; i++)
            {
                bool bottomPivot = chain[i] is "CharSlot_DepthScale" or "CharSlot_SwayPivot"
                    or "CharSlot_Scale" or "CharacterPortrait_VisualOffset";

                nodes.Add(new RigSchemaNodeDto
                {
                    id = chain[i],
                    parent = i == 0 ? "" : chain[i - 1],
                    anchoredPosition = F2(0f, 0f),
                    anchorMin = F2(0f, 0f),
                    anchorMax = F2(1f, 1f),
                    pivot = bottomPivot ? F2(0.5f, 0f) : F2(0.5f, 0.5f),
                    sizeDelta = F2(0f, 0f),
                    localScale = F3(1f, 1f, 1f),
                    localEulerAngles = F3(0f, 0f, 0f),
                });
            }

            return new StageReducerTuning
            {
                RigSchemas = new RigSchemasFileDto
                {
                    capturedUnderParentSize = F2(1920f, 1080f),
                    rigs = new List<RigSchemaRigDto>
                    {
                        new RigSchemaRigDto { rigKind = "character", nodes = nodes },
                    },
                },
                ReferenceStageWidth = 1920f,
                BaseResolution = new Vec2(1920f, 1080f),
                DepthPresets = MakeDepthPresets(),
                FocusTuning = MakeFocusTuning(),
            };
        }

        private static StageCommand Cmd(string name, params string[] args)
            => new StageCommand(name, args, "t.yarn:1");

        private static Vec2 FocusPointOf(StageState state, string slotKey, string presetName, StageReducerTuning tuning)
        {
            state.TryGetCharacter(slotKey, out string character);
            Vec2 offset = FocusOffsetMath.Resolve(tuning.FocusTuning, character, presetName, Vec2.Zero);

            RectNodeState[] chain = state.Nodes.BuildChainTo(
                StageState.NodeKeyOf(slotKey, "CharacterPortrait_VisualOffset"));

            return SettledFocusMath.FocusPointInRigSpace(chain, state.Nodes.RootSpace, offset);
        }

        [Test]
        public void place_center를_접으면_focus가_화면_가운데에_선다()
        {
            StageReducerTuning tuning = MakeTuning();
            StageState state = StageReducer.CreateInitialState(tuning);

            state = StageReducer.ApplyAll(state, new[]
            {
                Cmd("slot", "c3"),
                Cmd("cast", "c3", "parkeunseol"),
                Cmd("actor", "@3", "parkeunseol"),
                Cmd("place_center", "@3", "bust", "0fr"),
            }, tuning);

            Assert.That(state.Unhandled.Count(u => u.Command.Name == "place_center"), Is.EqualTo(0),
                string.Join("\n", state.Unhandled));

            Vec2 landed = FocusPointOf(state, "c3", "bust", tuning);

            Assert.That(landed.X, Is.EqualTo(0f).Within(Eps));
            Assert.That(landed.Y, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void place_left는_바깥_비율_지점이다()
        {
            StageReducerTuning tuning = MakeTuning();
            StageState state = StageReducer.CreateInitialState(tuning);

            state = StageReducer.ApplyAll(state, new[]
            {
                Cmd("slot", "c1"),
                Cmd("place_left", "c1", "face"),
            }, tuning);

            Vec2 landed = FocusPointOf(state, "c1", "face", tuning);

            Assert.That(landed.X, Is.EqualTo(-1920f * 0.24f).Within(Eps)); // -460.8 — 리포트의 그 값
            Assert.That(landed.Y, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void size를_접으면_focus는_제자리고_배율은_프리셋이다()
        {
            StageReducerTuning tuning = MakeTuning();
            StageState state = StageReducer.CreateInitialState(tuning);

            state = StageReducer.ApplyAll(state, new[]
            {
                Cmd("slot", "c3"),
                Cmd("cast", "c3", "parkeunseol"),
                Cmd("place_center", "c3", "bust"),
            }, tuning);

            Vec2 before = FocusPointOf(state, "c3", "bust", tuning);

            state = StageReducer.Apply(state, Cmd("size", "c3", "close", "bust", "0fr"), tuning);

            Assert.That(state.Unhandled.Count(u => u.Command.Name == "size"), Is.EqualTo(0),
                string.Join("\n", state.Unhandled));

            Vec2 after = FocusPointOf(state, "c3", "bust", tuning);

            Assert.That(after.X, Is.EqualTo(before.X).Within(Eps), "focus X 보존");
            Assert.That(after.Y, Is.EqualTo(before.Y).Within(Eps), "focus Y 보존");
            Assert.That(state.Nodes.GetState("c3/CharSlot_DepthScale").LocalScale.X,
                Is.EqualTo(1.38f).Within(1e-4f), "close 프리셋 배율");
        }

        [Test]
        public void 모르는_프리셋과_레벨_수치는_Unhandled다()
        {
            StageReducerTuning tuning = MakeTuning();
            StageState state = StageReducer.CreateInitialState(tuning);

            state = StageReducer.ApplyAll(state, new[]
            {
                Cmd("slot", "c1"),
                Cmd("size", "c1", "7", "bust"),        // 레벨 수치 — 커브 폴드 미지원
                Cmd("place_center", "c1", "elbow"),    // 모르는 focus 토큰
            }, tuning);

            Assert.That(state.Unhandled.Count, Is.EqualTo(2));
            Assert.That(state.Unhandled[0].Reason, Does.Contain("7"));
            Assert.That(state.Unhandled[1].Reason, Does.Contain("elbow"));
        }

        [Test]
        public void focus_오프셋은_base와_캐릭터_보정의_합이다()
        {
            Vec2 offset = FocusOffsetMath.Resolve(
                MakeFocusTuning(), "parkeunseol", "bust", Vec2.Zero);

            // base 820 + default -20 + bust 보정 10 = 810.
            Assert.That(offset.Y, Is.EqualTo(810f).Within(Eps));

            Vec2 noEntry = FocusOffsetMath.Resolve(MakeFocusTuning(), "unknown", "bust", Vec2.Zero);
            Assert.That(noEntry.Y, Is.EqualTo(820f).Within(Eps));
        }
    }
}
