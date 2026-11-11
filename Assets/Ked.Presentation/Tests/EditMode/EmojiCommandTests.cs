using System.Collections.Generic;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 이모지 커맨드(docs/PLAN_emoji.md §4) — 정의 읽기 · 대본 문법 · 폴드 · 장부.
    /// 리그가 서는 모양은 EmojiRigBuilderTests가 맡는다.
    /// </summary>
    public sealed class EmojiCommandTests
    {
        private readonly List<GameObject> _spawned = new();

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

        private static StageCommand Cmd(string name, params string[] args) => new(name, args, "test.yarn:1");

        // ── 정의(emojis.json) ────────────────────────────────────────

        private const string SampleJson = @"{
  ""schema"": ""emojis/1"",
  ""emojis"": [
    { ""name"": ""star"", ""offset"": { ""x"": 170, ""y"": 60 },
      ""rigs"": [ { ""name"": ""body"", ""sprite"": ""star"", ""place"": { ""x"": 0, ""y"": 0 } } ] },
    { ""name"": ""twinkle"",
      ""rigs"": [
        { ""name"": ""left"", ""sprite"": ""star"", ""place"": { ""x"": -120, ""y"": -20 } },
        { ""name"": ""right"", ""sprite"": ""star"", ""place"": { ""x"": 120, ""y"": -20 } } ] }
  ]
}";

        [Test]
        public void 정의를_읽는다_이름_오프셋_리그의_순서와_값()
        {
            EmojiLibrary library = EmojiLibrary.Parse(SampleJson, "test");

            Assert.That(library.Count, Is.EqualTo(2));

            Assert.That(library.TryGet("star", out EmojiDefinition star), Is.True);
            Assert.That(star.Offset.X, Is.EqualTo(170f));
            Assert.That(star.Offset.Y, Is.EqualTo(60f));
            Assert.That(star.Rigs, Has.Count.EqualTo(1));
            Assert.That(star.Rigs[0].Name, Is.EqualTo("body"));
            Assert.That(star.Rigs[0].SpriteKey, Is.EqualTo("star"));

            // offset을 생략하면 0이다.
            Assert.That(library.TryGet("twinkle", out EmojiDefinition twinkle), Is.True);
            Assert.That(twinkle.Offset.X, Is.EqualTo(0f));
            Assert.That(twinkle.Rigs[0].Name, Is.EqualTo("left"));
            Assert.That(twinkle.Rigs[1].Name, Is.EqualTo("right"));
            Assert.That(twinkle.Rigs[1].Place.X, Is.EqualTo(120f));

            Assert.That(library.TryGet("none", out _), Is.False);
        }

        [Test]
        public void 어긋난_이모지는_걸러지고_까닭이_남는다()
        {
            EmojisFileDto file = new()
            {
                schema = EmojiDefinitionLoader.Schema,
                emojis = new List<EmojiDto>
                {
                    new() { name = "ok", rigs = new List<EmojiRigDto> { new() { name = "a", sprite = "s" } } },
                    new() { name = "", rigs = new List<EmojiRigDto> { new() { name = "a" } } },
                    new() { name = "norig", rigs = new List<EmojiRigDto>() },
                    new() { name = "dup", rigs = new List<EmojiRigDto> { new() { name = "a" }, new() { name = "a" } } },
                    new() { name = "noname", rigs = new List<EmojiRigDto> { new() { name = " " } } },
                    new() { name = "ok", rigs = new List<EmojiRigDto> { new() { name = "b" } } },
                },
            };

            List<string> warned = new();
            Dictionary<string, EmojiDefinition> emojis = EmojiDefinitionLoader.Build(file, (name, why) => warned.Add(name));

            Assert.That(emojis.Keys, Is.EquivalentTo(new[] { "ok" }));
            Assert.That(emojis["ok"].Rigs[0].Name, Is.EqualTo("a"), "같은 이름이 둘이면 앞의 것이 남는다");
            Assert.That(warned, Has.Count.EqualTo(5));
        }

        // ── 대본 문법 ────────────────────────────────────────────────

        private static List<CommandSpecBase> Parse(StageCommand cmd)
        {
            List<CommandSpecBase> output = new();
            Assert.That(new StageCommandParser(null).TryParse(cmd, output), Is.True, cmd.Name);
            return output;
        }

        [Test]
        public void emoji는_대상과_이름을_스펙에_싣는다()
        {
            List<CommandSpecBase> specs = Parse(Cmd("emoji", "harin", " star "));

            Assert.That(specs, Has.Count.EqualTo(1));
            SetEmojiCommandSpec spec = (SetEmojiCommandSpec)specs[0];
            Assert.That(spec.roleKey, Is.EqualTo("harin"));
            Assert.That(spec.emojiName, Is.EqualTo("star"));
        }

        [Test]
        public void emoji에_이름이_없으면_경고하고_건너뛴다()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("emoji.*이름이 없다"));

            Assert.That(Parse(Cmd("emoji", "harin")), Is.Empty);
        }

        [Test]
        public void emoji_off는_이름이_없으면_전부를_뜻하는_빈_이름이다()
        {
            ClearEmojiCommandSpec one = (ClearEmojiCommandSpec)Parse(Cmd("emoji_off", "harin", "star"))[0];
            Assert.That(one.roleKey, Is.EqualTo("harin"));
            Assert.That(one.emojiName, Is.EqualTo("star"));

            ClearEmojiCommandSpec all = (ClearEmojiCommandSpec)Parse(Cmd("emoji_off", "harin"))[0];
            Assert.That(all.emojiName, Is.EqualTo(""));
        }

        // ── 폴드 ─────────────────────────────────────────────────────

        private static Float2Dto F2(float x, float y) => new() { x = x, y = y };
        private static Float3Dto F3(float x, float y, float z) => new() { x = x, y = y, z = z };

        private static StageReducerTuning NewTuning()
        {
            RigSchemaNodeDto root = new()
            {
                id = "__root",
                parent = "",
                anchoredPosition = F2(0f, 0f),
                anchorMin = F2(0f, 0f),
                anchorMax = F2(1f, 1f),
                pivot = F2(0.5f, 0.5f),
                sizeDelta = F2(0f, 0f),
                localScale = F3(1f, 1f, 1f),
                localEulerAngles = F3(0f, 0f, 0f),
                measuredRectSize = F2(0f, 0f),
            };

            return new StageReducerTuning
            {
                BaseResolution = new Vec2(1920f, 1080f),
                ReferenceStageWidth = 1920f,
                RigSchemas = new RigSchemasFileDto
                {
                    capturedUnderParentSize = F2(1920f, 1080f),
                    rigs = new List<RigSchemaRigDto>
                    {
                        new() { rigKind = "character", sourcePrefab = "", nodes = new List<RigSchemaNodeDto> { root } },
                    },
                },
                Emojis = EmojiLibrary.Parse(SampleJson, "test").Definitions,
            };
        }

        private static StageState Fold(params StageCommand[] commands)
        {
            StageReducerTuning tuning = NewTuning();
            return StageReducer.ApplyAll(StageReducer.CreateInitialState(tuning), commands, tuning);
        }

        [Test]
        public void 세우면_슬롯에_이모지가_선_순서대로_남는다()
        {
            StageState state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "star"),
                Cmd("emoji", "s1", "twinkle"));

            Assert.That(state.Unhandled, Is.Empty);
            Assert.That(state.EmojisOf("s1"), Is.EqualTo(new[] { "star", "twinkle" }));
            Assert.That(state.HasEmoji("s1", "star"), Is.True);
            Assert.That(state.EmojisOf("s2"), Is.Empty);
        }

        [Test]
        public void 서_있는_이모지를_다시_부르면_새로_선_것으로_친다()
        {
            StageState state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "star"),
                Cmd("emoji", "s1", "twinkle"),
                Cmd("emoji", "s1", "star"));

            Assert.That(state.Unhandled, Is.Empty);
            Assert.That(state.EmojisOf("s1"), Is.EqualTo(new[] { "twinkle", "star" }));
        }

        [Test]
        public void 이름을_대면_그것만_이름이_없으면_전부_내린다()
        {
            StageState state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "star"),
                Cmd("emoji", "s1", "twinkle"),
                Cmd("emoji_off", "s1", "star"));

            Assert.That(state.EmojisOf("s1"), Is.EqualTo(new[] { "twinkle" }));

            state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "star"),
                Cmd("emoji", "s1", "twinkle"),
                Cmd("emoji_off", "s1"));

            Assert.That(state.Unhandled, Is.Empty);
            Assert.That(state.EmojisOf("s1"), Is.Empty);

            // 아무것도 없어도 이름 없는 emoji_off는 조용하다.
            state = Fold(Cmd("slot", "s1"), Cmd("emoji_off", "s1"));
            Assert.That(state.Unhandled, Is.Empty);
        }

        [Test]
        public void 슬롯을_다시_선언하면_딸린_이모지도_사라진다()
        {
            StageState state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "star"),
                Cmd("slot", "s1"));

            Assert.That(state.EmojisOf("s1"), Is.Empty);
        }

        [Test]
        public void 못_접는_것은_Unhandled로_남는다()
        {
            // 정의에 없는 이모지 · 슬롯이 없는 대상 · 서 있지 않은 것을 내리기 · 이름 없는 emoji.
            StageState state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "nope"),
                Cmd("emoji", "ghost", "star"),
                Cmd("emoji_off", "s1", "star"),
                Cmd("emoji", "s1"));

            Assert.That(state.Unhandled, Has.Count.EqualTo(4));
            Assert.That(state.EmojisOf("s1"), Is.Empty);
        }

        [Test]
        public void 정의가_없으면_이모지는_전부_Unhandled다()
        {
            StageReducerTuning tuning = NewTuning();
            tuning.Emojis = null;

            StageState state = StageReducer.ApplyAll(
                StageReducer.CreateInitialState(tuning),
                new[] { Cmd("slot", "s1"), Cmd("emoji", "s1", "star") },
                tuning);

            Assert.That(state.Unhandled, Has.Count.EqualTo(1));
        }

        [Test]
        public void 복제한_상태의_이모지는_원본과_따로다()
        {
            StageState state = Fold(Cmd("slot", "s1"), Cmd("emoji", "s1", "star"));
            StageState clone = state.Clone();

            clone.AddEmoji("s1", "twinkle");

            Assert.That(state.EmojisOf("s1"), Is.EqualTo(new[] { "star" }));
            Assert.That(clone.EmojisOf("s1"), Is.EqualTo(new[] { "star", "twinkle" }));
        }

        // ── 장부 ─────────────────────────────────────────────────────

        private RectTransform NewSlotScale(string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            _spawned.Add(go);
            return (RectTransform)go.transform;
        }

        private static EmojiRefs Stand(EmojiRigRegistry registry, EmojiRigBuilder builder, RectTransform slotScale, string emoji)
        {
            RectTransform locator = builder.EnsureLocator(slotScale, "s1_");
            EmojiRefs refs = builder.BuildEmoji(locator, "s1_", emoji, new[] { "a" });
            registry.Register("s1", slotScale, refs);
            return refs;
        }

        [Test]
        public void 장부는_슬롯마다_이름으로_찾고_뺀다()
        {
            EmojiRigRegistry registry = new();
            EmojiRigBuilder builder = new();
            RectTransform slotScale = NewSlotScale("s1_CharSlot_Scale");

            EmojiRefs star = Stand(registry, builder, slotScale, "star");
            Stand(registry, builder, slotScale, "twinkle");

            Assert.That(registry.CountOf("s1", slotScale), Is.EqualTo(2));
            Assert.That(registry.TryGet("s1", slotScale, "star", out EmojiRefs found), Is.True);
            Assert.That(found, Is.SameAs(star));
            Assert.That(registry.TryGet("s2", slotScale, "star", out _), Is.False);

            Assert.That(registry.Remove("s1", slotScale, "star", out EmojiRefs removed), Is.True);
            Assert.That(removed, Is.SameAs(star));
            Assert.That(registry.Remove("s1", slotScale, "star", out _), Is.False);

            List<EmojiRefs> rest = new();
            registry.RemoveAll("s1", slotScale, rest);
            Assert.That(rest, Has.Count.EqualTo(1));
            Assert.That(registry.CountOf("s1", slotScale), Is.EqualTo(0));
        }

        [Test]
        public void 부서진_이모지와_옛_리그_밑의_이모지는_장부가_잊는다()
        {
            EmojiRigRegistry registry = new();
            EmojiRigBuilder builder = new();
            RectTransform oldScale = NewSlotScale("s1_CharSlot_Scale");

            EmojiRefs star = Stand(registry, builder, oldScale, "star");
            Stand(registry, builder, oldScale, "twinkle");

            // 부서진 것.
            builder.DestroyEmoji(star);
            Assert.That(registry.TryGet("s1", oldScale, "star", out _), Is.False);
            Assert.That(registry.CountOf("s1", oldScale), Is.EqualTo(1));

            // 슬롯이 다시 섰다 — 옛 리그는 아직 살아 있어도(프레임 끝에 부서진다) 그 밑의 것은 잊는다.
            RectTransform newScale = NewSlotScale("s1_CharSlot_Scale");
            Assert.That(registry.CountOf("s1", newScale), Is.EqualTo(0));
        }
    }
}
