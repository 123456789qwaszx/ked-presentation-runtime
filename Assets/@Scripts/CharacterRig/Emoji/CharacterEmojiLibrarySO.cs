using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CharacterEmojiEntry
{
    public string emojiKey;
    public Sprite sprite;

    public CharacterEmojiPlacement placement = CharacterEmojiPlacement.Default;
}

// 에디터용 Emoji 배치 프리셋
[Serializable]
public sealed class CharacterEmojiSavedPlacementSlot
{
    public string label = "New Placement";
    public CharacterEmojiPlacement placement = CharacterEmojiPlacement.Default;
}

[CreateAssetMenu(menuName = "CPS/CharRig/Emoji Library", fileName = "CharacterEmojiLibrary")]
public sealed class CharacterEmojiLibrarySO : ScriptableObject
{
    [SerializeField] private List<CharacterEmojiEntry> entries = new();

    [Header("Editor Placement Slots")]
    [SerializeField] private List<CharacterEmojiSavedPlacementSlot> savedPlacements = new();

    private Dictionary<string, CharacterEmojiEntry> _lookup;

    public bool TryGet(string emojiKey, out CharacterEmojiEntry entry)
    {
        EnsureLookup();

        emojiKey = (emojiKey ?? "").Trim();

        if (string.IsNullOrEmpty(emojiKey))
        {
            entry = null;
            return false;
        }

        return _lookup.TryGetValue(emojiKey, out entry) && entry != null;
    }

    private void EnsureLookup()
    {
        if (_lookup != null)
            return;

        _lookup = new Dictionary<string, CharacterEmojiEntry>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < entries.Count; i++)
        {
            CharacterEmojiEntry entry = entries[i];

            if (entry == null)
                continue;

            string key = (entry.emojiKey ?? "").Trim();

            if (string.IsNullOrEmpty(key))
                continue;

            _lookup[key] = entry;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _lookup = null;
    }
#endif
}
