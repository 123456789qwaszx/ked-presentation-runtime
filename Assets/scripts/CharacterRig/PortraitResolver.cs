using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PortraitResolver
{
    private readonly Dictionary<(string characterId, char variantSuffix, string emotionKey), Sprite> _map = new();

    public PortraitResolver(PortraitGeneratedDBSO db)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));
        if (db.entries == null) throw new InvalidOperationException("PortraitGeneratedDBSO.entries is null.");

        var entries = db.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var key = MakeKey(e.characterId, e.variantKey, e.emotionKey);

            if (!_map.TryAdd(key, e.sprite))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Duplicate portrait key ignored: {e.characterId}|{e.variantKey}|{e.emotionKey}");
#endif
            }
        }
    }

    public Sprite Resolve(string characterId, string variantKey, string emotionKey)
    {
        characterId = (characterId ?? "").Trim();
        variantKey  = (variantKey  ?? "").Trim();

        if (characterId.Length == 0 || variantKey.Length == 0)
            return null;

        emotionKey = NormalizeEmotionCode(emotionKey);
        if (emotionKey.Length == 0)
            return null;

        var key = MakeKey(characterId, variantKey, emotionKey);
        return _map.TryGetValue(key, out var sprite) && sprite ? sprite : null;
    }

    private static (string characterId, char variantSuffix, string emotionKey)
        MakeKey(string characterId, string variantKey, string emotionKey)
    {
        characterId = (characterId ?? "").Trim();
        variantKey  = (variantKey  ?? "").Trim();
        emotionKey  = (emotionKey  ?? "").Trim();

        char suffix = variantKey.Length > 0 ? variantKey[variantKey.Length - 1] : '\0';
        return (characterId, suffix, emotionKey);
    }

    // Normalizes the input into a 2-digit code in the form "02".
    // - "2"  → "02"
    // - "02" → "02"
    // - otherwise → "" (unsupported)
    public static string NormalizeEmotionCode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        input = input.Trim();

        if (input.Length == 2 && IsAsciiDigit(input[0]) && IsAsciiDigit(input[1]))
            return input;

        if (input.Length == 1 && IsAsciiDigit(input[0]))
            return "0" + input;

        return "";
    }

    private static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';
}
