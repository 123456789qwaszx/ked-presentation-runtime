using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CharacterEmojiEntry
{
    public string emojiKey;
    public Sprite sprite;
    public CharacterEmojiLayout layout = CharacterEmojiLayout.Default;
}

[CreateAssetMenu(
    menuName = "CPS/CharRig/Emoji Library",
    fileName = "CharacterEmojiLibrary")]
public sealed class CharacterEmojiLibrarySO : ScriptableObject
{
    [SerializeField] private List<CharacterEmojiEntry> entries = new();

    private Dictionary<string, CharacterEmojiEntry> _lookup;

    public bool TryGet(string emojiKey, out CharacterEmojiEntry entry)
    {
        EnsureLookup();

        if (string.IsNullOrEmpty(emojiKey))
        {
            entry = null;
            return false;
        }

        return _lookup.TryGetValue(emojiKey, out entry);
    }

    private void EnsureLookup()
    {
        if (_lookup != null)
            return;

        _lookup = new Dictionary<string, CharacterEmojiEntry>();

        for (int i = 0; i < entries.Count; i++)
        {
            CharacterEmojiEntry entry = entries[i];

            if (entry == null)
                continue;

            if (string.IsNullOrEmpty(entry.emojiKey))
                continue;

            _lookup[entry.emojiKey] = entry;
        }
    }
}
