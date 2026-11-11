using System;
using System.Collections.Generic;

namespace Ked.Presentation.Core
{
    // ─────────────────────────────────────────────────────────────────
    // emojis.json의 전송 타입 — 이모지의 구성(과제 2의 최소형, schema "emojis/1").
    //
    //   {
    //     "schema": "emojis/1",
    //     "emojis": [
    //       { "name": "star", "offset": { "x": 150, "y": 60 },
    //         "rigs": [ { "name": "body", "sprite": "star", "place": { "x": 0, "y": 0 } } ] }
    //     ]
    //   }
    //
    // 필드 이름이 JSON 키와 1:1이다. 코어는 JSON 파서를 갖지 않는다 — 역직렬화는 호스트가 한다
    // (유니티: JsonUtility, 툴: System.Text.Json + IncludeFields). 해석은 EmojiDefinitionLoader 한 곳이다.
    // offset · place는 px, y 위. 생략하면 0이다.
    // ─────────────────────────────────────────────────────────────────

    [Serializable]
    public sealed class EmojisFileDto
    {
        public string schema;
        public List<EmojiDto> emojis;
    }

    [Serializable]
    public sealed class EmojiDto
    {
        public string name;
        public Float2Dto offset;
        public List<EmojiRigDto> rigs;
    }

    [Serializable]
    public sealed class EmojiRigDto
    {
        public string name;
        public string sprite;
        public Float2Dto place;
    }

    /// <summary>DTO → 정의. 어긋난 이모지는 건너뛰고 까닭을 warn으로 넘긴다(이모지 이름, 까닭).</summary>
    public static class EmojiDefinitionLoader
    {
        public const string Schema = "emojis/1";

        public static Dictionary<string, EmojiDefinition> Build(EmojisFileDto file, Action<string, string> warn = null)
        {
            Dictionary<string, EmojiDefinition> result = new(StringComparer.Ordinal);

            if (file?.emojis == null)
                return result;

            for (int i = 0; i < file.emojis.Count; i++)
            {
                EmojiDto dto = file.emojis[i];

                if (dto == null)
                {
                    warn?.Invoke($"[{i}]", "항목이 비어 있다");
                    continue;
                }

                string label = string.IsNullOrEmpty(dto.name) ? $"[{i}]" : dto.name;

                List<EmojiRigEntry> rigs = new();

                if (dto.rigs != null)
                {
                    foreach (EmojiRigDto rig in dto.rigs)
                        rigs.Add(rig == null ? null : new EmojiRigEntry(rig.name, rig.sprite, ToVec2(rig.place)));
                }

                EmojiDefinition emoji = new(dto.name, ToVec2(dto.offset), rigs);

                if (!EmojiRules.TryValidate(emoji, out string why))
                {
                    warn?.Invoke(label, why);
                    continue;
                }

                if (result.ContainsKey(emoji.Name))
                {
                    warn?.Invoke(label, "같은 이름이 둘이다 — 뒤의 것을 무시한다");
                    continue;
                }

                result[emoji.Name] = emoji;
            }

            return result;
        }

        private static Vec2 ToVec2(Float2Dto value) => value == null ? Vec2.Zero : value.ToVec2();
    }
}
