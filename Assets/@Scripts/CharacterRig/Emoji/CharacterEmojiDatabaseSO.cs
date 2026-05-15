using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CharRig/Emoji Database", fileName = "EmojiDatabase")]
public sealed class CharacterEmojiDatabaseSO : ScriptableObject
{
    [Header("Emoji Presets")]
    public List<CharacterEmojiPresetSO> presets = new();

    private Dictionary<string, CharacterEmojiPresetSO> _map;

    public bool TryGet(string emojiKey, out CharacterEmojiPresetSO preset)
    {
        preset = null;

        if (string.IsNullOrWhiteSpace(emojiKey))
            return false;

        EnsureMap();

        string key = emojiKey.Trim();
        return _map.TryGetValue(key, out preset) && preset != null;
    }

    private void OnEnable() => InvalidateCache();
    private void OnValidate() => InvalidateCache();
    
    private void InvalidateCache() => _map = null;
    
    private void EnsureMap()
    {
        if (_map != null)
            return;

        RebuildMap();
    }
    
    private void RebuildMap()
    {
        _map = new Dictionary<string, CharacterEmojiPresetSO>(StringComparer.Ordinal);

        if (presets == null)
            return;

        for (int i = 0; i < presets.Count; i++)
        {
            CharacterEmojiPresetSO preset = presets[i];

            if (preset == null)
                continue;

            if (string.IsNullOrWhiteSpace(preset.emojiKey))
            {
                Debug.LogWarning(
                    $"[CharacterEmojiDatabaseSO] Empty emojiKey skipped. " +
                    $"index={i}, database='{name}'.");

                continue;
            }

            string key = preset.emojiKey.Trim();

            if (!_map.TryAdd(key, preset))
            {
                Debug.LogWarning(
                    $"[CharacterEmojiDatabaseSO] Duplicate emojiKey '{key}' found. " +
                    $"The later preset will override the previous one. " +
                    $"index={i}, database='{name}'.");

                _map[key] = preset;
            }
        }
    }
}