using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 이모지 리그 — 스키마대로 서는가(docs/emoji-rig.md §3 · §4), 이름 규칙, 다시 세우기와 부수기.
    /// 덤프와의 대조는 UnityParity/RigSchemaTreeUnityParityTests가 맡는다.
    /// </summary>
    public sealed class EmojiRigBuilderTests
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

        /// <summary>슬롯의 CharSlot_Scale 대역 — 무대 크기의 rect, 발 피벗.</summary>
        private RectTransform NewSlotScale()
        {
            GameObject go = new("harin_CharSlot_Scale", typeof(RectTransform));
            _spawned.Add(go);

            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1920f, 1080f);
            rt.pivot = new Vector2(0.5f, 0f);

            return rt;
        }

        private static EmojiRefs Build(EmojiRigBuilder builder, RectTransform slotScale, string emoji, params string[] rigs)
        {
            RectTransform locator = builder.EnsureLocator(slotScale, "harin_");
            return builder.BuildEmoji(locator, "harin_", emoji, rigs);
        }

        // ── 스키마 ───────────────────────────────────────────────────

        [Test]
        public void 스키마의_모든_노드가_Nodes에_한_번씩_있고_부모가_먼저_온다()
        {
            HashSet<EmojiRigSchema.Refs> seen = new();

            foreach (EmojiRigSchema.NodeDef node in EmojiRigSchema.Nodes)
            {
                if (node.Parent.HasValue)
                    Assert.That(seen, Does.Contain(node.Parent.Value), $"{node.Id}의 부모가 먼저 나오지 않았다");

                Assert.That(seen.Add(node.Id), Is.True, $"{node.Id}가 두 번 나온다");
            }

            Assert.That(seen.Count, Is.EqualTo(Enum.GetValues(typeof(EmojiRigSchema.Refs)).Length),
                "Refs에 있는데 Nodes에 없는 노드가 있다");
        }

        [Test]
        public void 값을_든_노드와_키를_받는_노드가_갈려_있다()
        {
            // 기본 자리(Place)는 값만 들고, 그림(Image)이 키를 받는다 — 두 단인 까닭.
            Assert.That(EmojiRigSchema.Get(EmojiRigSchema.Refs.EmojiRig_Place).ReceivesKeys, Is.False);
            Assert.That(EmojiRigSchema.Get(EmojiRigSchema.Refs.EmojiRig_Image).ReceivesKeys, Is.True);

            // 리그마다 되풀이되는 것은 그 두 단뿐이다.
            Assert.That(EmojiRigSchema.Get(EmojiRigSchema.Refs.Emoji_Locator).Repeat, Is.EqualTo(EmojiRigSchema.Repeat.PerSlot));
            Assert.That(EmojiRigSchema.Get(EmojiRigSchema.Refs.Emoji_Offset).Repeat, Is.EqualTo(EmojiRigSchema.Repeat.PerEmoji));
            Assert.That(EmojiRigSchema.Get(EmojiRigSchema.Refs.EmojiRig_Place).Repeat, Is.EqualTo(EmojiRigSchema.Repeat.PerRig));
            Assert.That(EmojiRigSchema.Get(EmojiRigSchema.Refs.EmojiRig_Image).Repeat, Is.EqualTo(EmojiRigSchema.Repeat.PerRig));
        }

        // ── 세우기 ───────────────────────────────────────────────────

        [Test]
        public void 리그_이름마다_두_단이_스키마의_부모대로_선다()
        {
            RectTransform slotScale = NewSlotScale();
            EmojiRefs emoji = Build(new EmojiRigBuilder(), slotScale, "flower", "big", "small");

            Assert.That(emoji.Emoji_Locator.parent, Is.SameAs(slotScale));
            Assert.That(emoji.Emoji_Offset.parent, Is.SameAs(emoji.Emoji_Locator));
            Assert.That(emoji.Rigs, Has.Count.EqualTo(2));

            foreach (EmojiRigRefs rig in emoji.Rigs)
            {
                Assert.That(rig.EmojiRig_Place.parent, Is.SameAs(emoji.Emoji_Offset), rig.Name);
                Assert.That(rig.EmojiRig_Image.parent, Is.SameAs(rig.EmojiRig_Place), rig.Name);
                Assert.That(rig.Image, Is.Not.Null, rig.Name);
                Assert.That(rig.CanvasGroup, Is.Not.Null, rig.Name);
            }

            // 세운 순서가 그려지는 순서다.
            Assert.That(emoji.Rigs[0].Name, Is.EqualTo("big"));
            Assert.That(emoji.Rigs[0].EmojiRig_Place.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(emoji.Rigs[1].EmojiRig_Place.GetSiblingIndex(), Is.EqualTo(1));

            // 로케이터 + 오프셋 + 리그 둘 × 두 단 — 그 밖의 노드가 없다.
            Assert.That(emoji.Emoji_Locator.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(6));
        }

        [Test]
        public void 노드의_모양이_스키마와_같다()
        {
            RectTransform slotScale = NewSlotScale();
            EmojiRefs emoji = Build(new EmojiRigBuilder(), slotScale, "star", "body");
            EmojiRigRefs rig = emoji.Rigs[0];

            // 로케이터는 발(부모 rect의 아래 가운데)에 붙는다.
            Assert.That(emoji.Emoji_Locator.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(emoji.Emoji_Locator.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));

            foreach (RectTransform node in new[] { emoji.Emoji_Locator, emoji.Emoji_Offset, rig.EmojiRig_Place, rig.EmojiRig_Image })
            {
                Assert.That(node.anchorMin, Is.EqualTo(node.anchorMax), $"{node.name}: 점에 붙는다(늘어나지 않는다)");
                Assert.That(node.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)), $"{node.name}: 피벗은 가운데");
                Assert.That(node.anchoredPosition, Is.EqualTo(Vector2.zero), $"{node.name}: 중립은 제자리");
                Assert.That(node.localScale, Is.EqualTo(Vector3.one), node.name);
            }

            // 그림 노드만 크기 · Image · CanvasGroup이 있다.
            Assert.That(rig.EmojiRig_Image.sizeDelta, Is.EqualTo(EmojiRigSchema.DefaultImageSize));
            Assert.That(rig.EmojiRig_Place.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(rig.EmojiRig_Place.GetComponent<Image>(), Is.Null);
            Assert.That(emoji.Emoji_Offset.GetComponent<Image>(), Is.Null);

            Assert.That(rig.CanvasGroup.alpha, Is.EqualTo(1f));
            Assert.That(rig.Image.raycastTarget, Is.False, "이모지는 클릭을 받지 않는다");
        }

        [Test]
        public void 하이어라키_이름에_슬롯_이모지_리그가_접두로_붙는다()
        {
            EmojiRefs emoji = Build(new EmojiRigBuilder(), NewSlotScale(), "flower", "big");
            EmojiRigRefs rig = emoji.Rigs[0];

            Assert.That(emoji.Emoji_Locator.name, Is.EqualTo("harin_Emoji_Locator"));
            Assert.That(emoji.Emoji_Offset.name, Is.EqualTo("harin_flower_Emoji_Offset"));
            Assert.That(rig.EmojiRig_Place.name, Is.EqualTo("harin_flower_big_EmojiRig_Place"));
            Assert.That(rig.EmojiRig_Image.name, Is.EqualTo("harin_flower_big_EmojiRig_Image"));
        }

        [Test]
        public void 리그를_이름으로_찾는다()
        {
            EmojiRefs emoji = Build(new EmojiRigBuilder(), NewSlotScale(), "flower", "big", "small");

            Assert.That(emoji.TryGetRig("small", out EmojiRigRefs small), Is.True);
            Assert.That(small, Is.SameAs(emoji.Rigs[1]));
            Assert.That(emoji.TryGetRig("none", out _), Is.False);
            Assert.That(emoji.TryGetRig(null, out _), Is.False);
        }

        // ── 로케이터 ─────────────────────────────────────────────────

        [Test]
        public void 로케이터는_슬롯에_하나다_이모지_여럿이_그_밑에_선다()
        {
            RectTransform slotScale = NewSlotScale();
            EmojiRigBuilder builder = new();

            EmojiRefs a = Build(builder, slotScale, "star", "body");
            EmojiRefs b = Build(builder, slotScale, "question", "mark");

            Assert.That(b.Emoji_Locator, Is.SameAs(a.Emoji_Locator));
            Assert.That(slotScale.childCount, Is.EqualTo(1));
            Assert.That(a.Emoji_Locator.childCount, Is.EqualTo(2));
        }

        [Test]
        public void 로케이터는_초상보다_뒤에_그린다()
        {
            RectTransform slotScale = NewSlotScale();
            EmojiRigBuilder builder = new();

            RectTransform locator = builder.EnsureLocator(slotScale, "harin_");

            // 로케이터가 선 뒤에 형제가 늘어도, 다시 받으면 맨 뒤로 간다.
            GameObject sibling = new("harin_CharacterPortrait_VisualOffset", typeof(RectTransform));
            sibling.transform.SetParent(slotScale, false);

            Assert.That(builder.EnsureLocator(slotScale, "harin_"), Is.SameAs(locator));
            Assert.That(locator.GetSiblingIndex(), Is.EqualTo(slotScale.childCount - 1));
        }

        // ── 다시 세우기 · 부수기 ─────────────────────────────────────

        [Test]
        public void 같은_이름의_이모지를_다시_세우면_옛것을_부수고_새로_선다()
        {
            RectTransform slotScale = NewSlotScale();
            EmojiRigBuilder builder = new();

            EmojiRefs first = Build(builder, slotScale, "flower", "a", "b", "c");
            RectTransform oldOffset = first.Emoji_Offset;

            EmojiRefs second = Build(builder, slotScale, "flower", "a");

            Assert.That(oldOffset == null, Is.True, "옛 오프셋이 부서지지 않았다");
            Assert.That(second.Emoji_Locator.childCount, Is.EqualTo(1));
            Assert.That(second.Rigs, Has.Count.EqualTo(1));
        }

        [Test]
        public void 부수면_이모지는_사라지고_로케이터는_남는다()
        {
            RectTransform slotScale = NewSlotScale();
            EmojiRigBuilder builder = new();

            EmojiRefs emoji = Build(builder, slotScale, "flower", "a", "b");
            RectTransform locator = emoji.Emoji_Locator;

            builder.DestroyEmoji(emoji);

            Assert.That(locator != null, Is.True);
            Assert.That(locator.childCount, Is.EqualTo(0));

            // 두 번 부숴도, 빈 것을 부숴도 터지지 않는다.
            Assert.DoesNotThrow(() => builder.DestroyEmoji(emoji));
            Assert.DoesNotThrow(() => builder.DestroyEmoji(null));
        }

        // ── 막는 것 ──────────────────────────────────────────────────

        [Test]
        public void 이름이_비거나_리그가_없거나_리그_이름이_겹치면_막는다()
        {
            RectTransform slotScale = NewSlotScale();
            EmojiRigBuilder builder = new();
            RectTransform locator = builder.EnsureLocator(slotScale, "harin_");

            Assert.Throws<ArgumentException>(() => builder.BuildEmoji(locator, "harin_", "", new[] { "a" }));
            Assert.Throws<ArgumentException>(() => builder.BuildEmoji(locator, "harin_", "flower", Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => builder.BuildEmoji(locator, "harin_", "flower", null));
            Assert.Throws<ArgumentException>(() => builder.BuildEmoji(locator, "harin_", "flower", new[] { "a", " " }));
            Assert.Throws<ArgumentException>(() => builder.BuildEmoji(locator, "harin_", "flower", new[] { "a", "a" }));

            // 막힌 뒤에 남은 것이 없다.
            Assert.That(locator.childCount, Is.EqualTo(0));
        }

        // ── 틀의 견본 ────────────────────────────────────────────────

        [Test]
        public void 틀의_견본은_노드_이름이_스키마의_id_그대로다()
        {
            GameObject stageGo = new("__stage", typeof(RectTransform));
            _spawned.Add(stageGo);

            EmojiRefs template = new EmojiRigBuilder().BuildTemplate((RectTransform)stageGo.transform);

            Assert.That(template.Emoji_Locator.name, Is.EqualTo(nameof(EmojiRigSchema.Refs.Emoji_Locator)));
            Assert.That(template.Emoji_Offset.name, Is.EqualTo(nameof(EmojiRigSchema.Refs.Emoji_Offset)));
            Assert.That(template.Rigs, Has.Count.EqualTo(1));
            Assert.That(template.Rigs[0].EmojiRig_Place.name, Is.EqualTo(nameof(EmojiRigSchema.Refs.EmojiRig_Place)));
            Assert.That(template.Rigs[0].EmojiRig_Image.name, Is.EqualTo(nameof(EmojiRigSchema.Refs.EmojiRig_Image)));

            // 덤프에 실리는 노드 수 = 스키마의 노드 수.
            Assert.That(
                template.Emoji_Locator.GetComponentsInChildren<RectTransform>(true).Length,
                Is.EqualTo(EmojiRigSchema.Nodes.Length));
        }
    }
}
