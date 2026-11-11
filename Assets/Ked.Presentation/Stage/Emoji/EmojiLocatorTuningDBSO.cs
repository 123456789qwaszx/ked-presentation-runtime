using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────
// 이모지 로케이터의 자리 — 캐릭터 + 바리에이션(포즈)별 표.
//
// 이모지는 캐릭터를 모른다. 캐릭터마다 머리가 어디인가는 이 표가 들고, 그 값이 Emoji_Locator에 들어간다
// (경계 Emoji_Offset의 위 — docs/emoji-rig.md §3). 캐릭터의 그림 보정(RoleAnchorTuningDBSO)은
// 초상의 형제인 이모지에 전해지지 않으므로 이 표가 그것을 대신한다.
//
// 찾는 순서: 캐릭터 + 포즈 → 캐릭터(포즈 칸이 빈 항목) → 없다(부르는 쪽이 기본값을 쓴다).
// 포즈마다 잴 필요가 없다 — 선 포즈가 여럿이면 캐릭터 항목 하나로 덮고, 머리가 내려가는 포즈만 따로 적는다.
// ─────────────────────────────────────────────────────────────────────
[CreateAssetMenu(menuName = "CPS/Emoji/Tuning/Emoji Locator Tuning DB", fileName = "EmojiLocatorTuningDB")]
public sealed class EmojiLocatorTuningDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("캐릭터 키(예: seina). 대소문자는 가리지 않는다.")]
        public string character;

        [Tooltip("포즈(변형) 키. 비우면 그 캐릭터의 모든 포즈에 쓰인다 — 포즈를 적은 항목이 있으면 그것이 이긴다.")]
        public string variant;

        [Tooltip("로케이터의 자리 — 인물의 발(슬롯의 아래 가운데)에서 잰 px, y 위. 머리 높이쯤으로 둔다.")]
        public Vector2 position = new(0f, 800f);
    }

    public List<Entry> entries = new();

    private sealed class CharacterEntries
    {
        public bool HasBase;
        public Vector2 Base;
        public readonly Dictionary<string, Vector2> ByVariant = new(StringComparer.Ordinal);
    }

    private Dictionary<string, CharacterEntries> _map;

    /// <summary>
    /// 캐릭터와 포즈의 자리를 찾는다. 포즈 항목 → 캐릭터 항목 순서다. 둘 다 없으면 false — 부르는 쪽이 기본값을 쓴다
    /// (항목이 없는 것은 오류가 아니라 "기본 자리의 캐릭터"라는 뜻이다).
    /// </summary>
    public bool TryResolve(string characterKey, string variantKey, out Vector2 position)
    {
        if (_map == null)
            Build();

        position = default;

        string character = PresentationKeyNormalizer.NormalizeCharacterKey(characterKey);

        if (character.Length == 0 || !_map.TryGetValue(character, out CharacterEntries found))
            return false;

        string variant = PresentationKeyNormalizer.NormalizeVariantKey(variantKey);

        if (variant.Length > 0 && found.ByVariant.TryGetValue(variant, out position))
            return true;

        if (!found.HasBase)
            return false;

        position = found.Base;
        return true;
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    // 키는 초상과 같은 정규화를 거친다(CastRegistry가 든 값과 같은 모양). 같은 키가 둘이면 나중 것이 이긴다.
    private void Build()
    {
        _map = new Dictionary<string, CharacterEntries>(StringComparer.Ordinal);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];

            if (entry == null)
                continue;

            string character = PresentationKeyNormalizer.NormalizeCharacterKey(entry.character);

            if (character.Length == 0)
                continue;

            if (!_map.TryGetValue(character, out CharacterEntries found))
            {
                found = new CharacterEntries();
                _map[character] = found;
            }

            string variant = PresentationKeyNormalizer.NormalizeVariantKey(entry.variant);

#if UNITY_EDITOR
            bool duplicate = variant.Length == 0 ? found.HasBase : found.ByVariant.ContainsKey(variant);

            if (duplicate)
                Debug.LogWarning($"[EmojiLocatorTuningDB] Duplicate key: '{character}' / '{variant}'", this);
#endif

            if (variant.Length == 0)
            {
                found.HasBase = true;
                found.Base = entry.position;
            }
            else
            {
                found.ByVariant[variant] = entry.position;
            }
        }
    }
}
