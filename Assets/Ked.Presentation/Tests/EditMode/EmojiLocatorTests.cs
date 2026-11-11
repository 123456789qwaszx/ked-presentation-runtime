using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 이모지 로케이터의 자리(docs/PLAN_emoji-placement.md §2) — 캐릭터 + 포즈별 표를 찾는 순서,
    /// 표에 없을 때의 기본값, 서 있는 동안 포즈가 바뀌면 따라 옮겨지는 것.
    /// </summary>
    public sealed class EmojiLocatorTests
    {
        private static readonly Vector2 Default = new(0f, 800f);

        private readonly List<Object> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _spawned.Clear();
        }

        private EmojiLocatorTuningDBSO NewTable(params (string character, string variant, Vector2 position)[] entries)
        {
            EmojiLocatorTuningDBSO table = ScriptableObject.CreateInstance<EmojiLocatorTuningDBSO>();
            _spawned.Add(table);

            foreach ((string character, string variant, Vector2 position) in entries)
            {
                table.entries.Add(new EmojiLocatorTuningDBSO.Entry
                {
                    character = character,
                    variant = variant,
                    position = position,
                });
            }

            return table;
        }

        private RectTransform NewLocator(string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            _spawned.Add(go);
            return (RectTransform)go.transform;
        }

        private static PresentationStage StageWith(string slotKey, string character, string variant = null)
        {
            PresentationStage stage = new();
            stage.castRegistry.CastCharRig(slotKey, character);

            if (variant != null)
                stage.castRegistry.SetVariant(slotKey, variant);

            return stage;
        }

        // ── 표 ───────────────────────────────────────────────────────

        [Test]
        public void 포즈_항목이_있으면_그것을_없으면_캐릭터_항목을_쓴다()
        {
            EmojiLocatorTuningDBSO table = NewTable(
                ("harin", "", new Vector2(0f, 900f)),
                ("harin", "sit", new Vector2(20f, 600f)));

            Assert.That(table.TryResolve("harin", "sit", out Vector2 sit), Is.True);
            Assert.That(sit, Is.EqualTo(new Vector2(20f, 600f)));

            // 표에 없는 포즈 · 포즈가 없는 것 — 캐릭터 항목.
            Assert.That(table.TryResolve("harin", "school", out Vector2 school), Is.True);
            Assert.That(school, Is.EqualTo(new Vector2(0f, 900f)));

            Assert.That(table.TryResolve("harin", "", out Vector2 none), Is.True);
            Assert.That(none, Is.EqualTo(new Vector2(0f, 900f)));
        }

        [Test]
        public void 포즈_항목만_있는_캐릭터는_다른_포즈에서_없는_것이다()
        {
            EmojiLocatorTuningDBSO table = NewTable(("harin", "sit", new Vector2(20f, 600f)));

            Assert.That(table.TryResolve("harin", "sit", out _), Is.True);
            Assert.That(table.TryResolve("harin", "school", out _), Is.False);
            Assert.That(table.TryResolve("harin", "", out _), Is.False);
        }

        [Test]
        public void 키는_대소문자와_앞뒤_공백을_가리지_않는다()
        {
            EmojiLocatorTuningDBSO table = NewTable((" Harin ", "Sit", new Vector2(20f, 600f)));

            Assert.That(table.TryResolve("HARIN", " sit", out Vector2 found), Is.True);
            Assert.That(found, Is.EqualTo(new Vector2(20f, 600f)));
        }

        [Test]
        public void 없는_캐릭터와_빈_키는_없는_것이다()
        {
            EmojiLocatorTuningDBSO table = NewTable(("harin", "", new Vector2(0f, 900f)));

            Assert.That(table.TryResolve("shigure", "", out _), Is.False);
            Assert.That(table.TryResolve("", "", out _), Is.False);
            Assert.That(table.TryResolve(null, null, out _), Is.False);
        }

        // ── 자리를 푸는 곳 ───────────────────────────────────────────

        [Test]
        public void 슬롯에_선_캐릭터와_포즈로_자리를_푼다()
        {
            EmojiLocatorTuningDBSO table = NewTable(
                ("harin", "", new Vector2(0f, 900f)),
                ("harin", "sit", new Vector2(20f, 600f)));

            EmojiLocatorSystem standing = new(StageWith("s1", "Harin"), table, Default);
            Assert.That(standing.Resolve("s1"), Is.EqualTo(new Vector2(0f, 900f)));

            EmojiLocatorSystem sitting = new(StageWith("s1", "Harin", "sit"), table, Default);
            Assert.That(sitting.Resolve("s1"), Is.EqualTo(new Vector2(20f, 600f)));
        }

        [Test]
        public void 표가_없거나_표에_없는_캐릭터이거나_캐스팅_전이면_기본_자리다()
        {
            EmojiLocatorTuningDBSO table = NewTable(("harin", "", new Vector2(0f, 900f)));

            // 표가 없는 호스트.
            Assert.That(new EmojiLocatorSystem(StageWith("s1", "harin"), null, Default).Resolve("s1"), Is.EqualTo(Default));

            // 표에 없는 캐릭터.
            Assert.That(new EmojiLocatorSystem(StageWith("s1", "shigure"), table, Default).Resolve("s1"), Is.EqualTo(Default));

            // 캐스팅되지 않은 슬롯 — 경고도 없다.
            Assert.That(new EmojiLocatorSystem(new PresentationStage(), table, Default).Resolve("s1"), Is.EqualTo(Default));
        }

        [Test]
        public void 놓으면_자리가_들어가고_포즈가_바뀌면_따라_옮겨진다()
        {
            EmojiLocatorTuningDBSO table = NewTable(
                ("harin", "", new Vector2(0f, 900f)),
                ("harin", "sit", new Vector2(20f, 600f)));

            PresentationStage stage = StageWith("s1", "harin");
            EmojiLocatorSystem locators = new(stage, table, Default);
            RectTransform locator = NewLocator("s1_Emoji_Locator");

            locators.Place("s1", locator);
            Assert.That(locator.anchoredPosition, Is.EqualTo(new Vector2(0f, 900f)));

            // 포즈가 바뀌어도 통보는 없다 — 다음 Refresh가 다시 푼다.
            stage.castRegistry.SetVariant("s1", "sit");
            Assert.That(locator.anchoredPosition, Is.EqualTo(new Vector2(0f, 900f)));

            locators.Refresh();
            Assert.That(locator.anchoredPosition, Is.EqualTo(new Vector2(20f, 600f)));

            stage.castRegistry.SetVariant("s1", "school");
            locators.Refresh();
            Assert.That(locator.anchoredPosition, Is.EqualTo(new Vector2(0f, 900f)));
        }

        [Test]
        public void 슬롯의_캐릭터가_바뀌면_그_캐릭터의_자리로_옮겨진다()
        {
            EmojiLocatorTuningDBSO table = NewTable(
                ("harin", "", new Vector2(0f, 900f)),
                ("shigure", "", new Vector2(0f, 1000f)));

            PresentationStage stage = StageWith("s1", "harin");
            EmojiLocatorSystem locators = new(stage, table, Default);
            RectTransform locator = NewLocator("s1_Emoji_Locator");

            locators.Place("s1", locator);

            stage.castRegistry.CastCharRig("s1", "shigure");
            locators.Refresh();

            Assert.That(locator.anchoredPosition, Is.EqualTo(new Vector2(0f, 1000f)));
        }

        [Test]
        public void 슬롯마다_하나만_기억하고_부서진_로케이터는_잊는다()
        {
            PresentationStage stage = StageWith("s1", "harin");
            EmojiLocatorSystem locators = new(stage, null, Default);

            RectTransform first = NewLocator("s1_Emoji_Locator");
            RectTransform second = NewLocator("s1_Emoji_Locator");

            // 슬롯이 다시 서서 로케이터가 새로 섰다 — 같은 슬롯은 덮는다.
            locators.Place("s1", first);
            locators.Place("s1", second);
            Assert.That(locators.Count, Is.EqualTo(1));

            Object.DestroyImmediate(second.gameObject);
            Assert.DoesNotThrow(() => locators.Refresh());
            Assert.That(locators.Count, Is.EqualTo(0));
        }

        [Test]
        public void 재생의_Tick이_로케이터의_자리를_다시_푼다()
        {
            EmojiLocatorTuningDBSO table = NewTable(
                ("harin", "", new Vector2(0f, 900f)),
                ("harin", "sit", new Vector2(20f, 600f)));

            PresentationStage stage = StageWith("s1", "harin");
            EmojiLocatorSystem locators = new(stage, table, Default);
            EmojiPlaybackSystem playback = new(new EmojiRigBuilder(), locators);
            RectTransform locator = NewLocator("s1_Emoji_Locator");

            locators.Place("s1", locator);
            stage.castRegistry.SetVariant("s1", "sit");

            playback.Tick(0f);

            Assert.That(locator.anchoredPosition, Is.EqualTo(new Vector2(20f, 600f)));
        }
    }
}
