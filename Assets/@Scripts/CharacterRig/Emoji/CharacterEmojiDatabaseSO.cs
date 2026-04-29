using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CPS/CharRig/Emoji Database", fileName = "EmojiDatabase")]
public sealed class CharacterEmojiDatabaseSO : ScriptableObject
{
    public List<CharacterEmojiPresetSO> presets = new();

    private Dictionary<string, CharacterEmojiPresetSO> _map;

    public bool TryGet(string emojiKey, out CharacterEmojiPresetSO preset)
    {
        if (_map == null) BuildMap();
        return _map.TryGetValue(emojiKey, out preset) && preset != null;
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    private void BuildMap()
    {
        _map = new Dictionary<string, CharacterEmojiPresetSO>(StringComparer.Ordinal);
        for (int i = 0; i < presets.Count; i++)
        {
            var p = presets[i];
            if (p == null || string.IsNullOrWhiteSpace(p.emojiKey)) continue;
            _map[p.emojiKey.Trim()] = p;
        }
    }
}