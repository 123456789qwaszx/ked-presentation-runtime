using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InlineEmojiResolver : MonoBehaviour
{
    [Header("Cue -> SpriteKey")]
    [SerializeField] private List<EmojiCueMapEntry> cueMap = new();

    private Dictionary<string, string> _cueToSpriteKey;

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildMap();
    }
#endif

    private void Awake()
    {
        RebuildMap();
    }

    public bool TryResolveEmoji(string cue, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrWhiteSpace(cue))
            return false;

        string spriteKey = ResolveCueToSpriteKey(cue);

        if (string.IsNullOrWhiteSpace(spriteKey))
            return false;

        sprite = Resources.Load<Sprite>(spriteKey);
        return sprite != null;
    }

    private string ResolveCueToSpriteKey(string cue)
    {
        if (_cueToSpriteKey == null)
            return cue;

        if (_cueToSpriteKey.TryGetValue(cue, out string spriteKey))
            return spriteKey;

        return cue;
    }

    private void RebuildMap()
    {
        _cueToSpriteKey = new Dictionary<string, string>(StringComparer.Ordinal);

        if (cueMap == null)
            return;

        for (int i = 0; i < cueMap.Count; i++)
        {
            EmojiCueMapEntry entry = cueMap[i];

            if (string.IsNullOrWhiteSpace(entry.cue))
                continue;

            _cueToSpriteKey[entry.cue] = entry.spriteKey;
        }
    }
}