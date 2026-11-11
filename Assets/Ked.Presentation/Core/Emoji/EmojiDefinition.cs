using System;
using System.Collections.Generic;

namespace Ked.Presentation.Core
{
    // ─────────────────────────────────────────────────────────────────
    // 이모지의 구성 — 툴이 정하는 것(docs/emoji-rig.md §5).
    //
    // 이모지 = 자기 오프셋 + 리그 여러 개. 리그 하나 = 이름 · 스프라이트 · 기본 자리.
    // 리그의 틀(노드와 채널)은 런타임의 EmojiRigSchema가 정하고, 여기에는 틀이 없다 —
    // "이 이모지에 리그가 몇 개이고 이름이 무엇인가"만 있다.
    //
    // 과제 2의 최소형이다. 키(타임라인 · 구간)는 과제 3에서 여기에 붙는다.
    // 재생(런타임 EmojiLibrary)과 폴드(StageReducer.Emoji)가 같은 정의를 본다.
    // ─────────────────────────────────────────────────────────────────

    /// <summary>이모지 안의 리그 하나.</summary>
    public sealed class EmojiRigEntry
    {
        /// <summary>이모지 안에서 리그를 가리키는 이름. 툴이 붙인다 — 키가 이 이름으로 리그를 찾는다.</summary>
        public string Name { get; }

        /// <summary>스프라이트 키 — Resources/Emojis/{키}.</summary>
        public string SpriteKey { get; }

        /// <summary>기본 자리(EmojiRig_Place). 오프셋 기준 px, y 위.</summary>
        public Vec2 Place { get; }

        public EmojiRigEntry(string name, string spriteKey, Vec2 place)
        {
            Name = name;
            SpriteKey = spriteKey;
            Place = place;
        }
    }

    /// <summary>이모지 하나의 구성.</summary>
    public sealed class EmojiDefinition
    {
        /// <summary>대본이 부르는 이름.</summary>
        public string Name { get; }

        /// <summary>이모지별 오프셋(Emoji_Offset). 로케이터(RootSpace) 기준 px, y 위.</summary>
        public Vec2 Offset { get; }

        /// <summary>리그들 — 순서가 그려지는 순서다(뒤가 위).</summary>
        public IReadOnlyList<EmojiRigEntry> Rigs { get; }

        public EmojiDefinition(string name, Vec2 offset, IReadOnlyList<EmojiRigEntry> rigs)
        {
            Name = name;
            Offset = offset;
            Rigs = rigs ?? Array.Empty<EmojiRigEntry>();
        }
    }

    /// <summary>
    /// 이모지 정의의 모양 규칙 — 읽는 쪽(런타임 로더)과 내는 쪽(툴)이 같은 규칙을 쓴다.
    /// 어긋난 정의는 읽을 때 걸러진다("툴에서는 되는데 재생에서 사라지는" 이모지가 안 생기게).
    /// </summary>
    public static class EmojiRules
    {
        public static bool TryValidate(EmojiDefinition emoji, out string why)
        {
            if (emoji == null)
            {
                why = "정의가 비어 있다";
                return false;
            }

            if (string.IsNullOrWhiteSpace(emoji.Name))
            {
                why = "이모지에 이름이 없다";
                return false;
            }

            if (emoji.Rigs.Count == 0)
            {
                why = "리그가 없다 — 리그는 하나 이상이어야 한다";
                return false;
            }

            HashSet<string> seen = new(StringComparer.Ordinal);

            for (int i = 0; i < emoji.Rigs.Count; i++)
            {
                EmojiRigEntry rig = emoji.Rigs[i];

                if (rig == null || string.IsNullOrWhiteSpace(rig.Name))
                {
                    why = $"리그 [{i}]에 이름이 없다";
                    return false;
                }

                if (!seen.Add(rig.Name))
                {
                    why = $"리그 이름 '{rig.Name}'이 두 번 나온다 — 키가 리그를 이름으로 가리킨다";
                    return false;
                }
            }

            why = null;
            return true;
        }
    }
}
