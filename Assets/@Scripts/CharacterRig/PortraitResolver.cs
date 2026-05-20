using System.Collections.Generic;
using UnityEngine;

public sealed class PortraitResolver
{
    public const string DefaultVariant = "a";
    public const string DefaultEmotion = "02";
    public const string FallbackEmotion = "01";

    private readonly Dictionary<(string characterId, char variantSuffix, string emotionKey), Sprite> _map = new();

    public PortraitResolver(PortraitGeneratedDbSo db)
    {
        BuildPortraitMap(db);
    }
    
    private void BuildPortraitMap(PortraitGeneratedDbSo db)
    {
        var entries = db.entries;
        
        for (int i = 0; i < entries.Count; i++)
        {
            PortraitGeneratedDbSo.Entry e = entries[i];
            
            var key = MakeKey(e.characterId, e.variantKey, e.emotionKey);

            if (!_map.TryAdd(key, e.sprite))
                Debug.LogWarning($"[PortraitResolver] Duplicate portrait key ignored. index={i}, key='{key}'");
        }
    }

    public Sprite Resolve(CommandRunScope scope, string targetKey, PortraitIdentity portrait, string debugName)
    {
        string roleKey = CharacterRigTargetResolver.ResolveSlotKeyFromTargetKey(scope, targetKey);

        string character = portrait.character;
        string variant = portrait.variant;
        string emotion = portrait.emotion;
        
        // Empty character/variant are resolved from the character cast to this slot.
        if (string.IsNullOrEmpty(character) || string.IsNullOrEmpty(variant))
        {
            scope.CastRegistry.TryGetCharacter(roleKey, out string characterKey);
                character = characterKey;
                
            scope.CastRegistry.TryGetVariant(roleKey, out string variantKey);
                variant = variantKey;
        }
        
        // Character-only portrait calls fall back to the default variant.
        if (string.IsNullOrEmpty(variant))
            variant = DefaultVariant;
        
        if (string.IsNullOrEmpty(emotion))
            emotion = DefaultEmotion;

        var key = MakeKey(character, variant, emotion);

        if (!_map.TryGetValue(key, out Sprite sprite) || sprite == null)
        {
            Debug.LogWarning(
                $"[{debugName}] Failed to resolve portrait. " +
                $"targetKey='{targetKey}', roleKey='{roleKey}', " +
                $"character='{character}', variant='{variant}', " +
                $"emotion='{emotion}'.");

            if (_map.TryGetValue(MakeKey(character, DefaultVariant, FallbackEmotion), out sprite))
                return sprite;
            
            return null;
        }

        return sprite;
    }

    private static (string characterId, char variantSuffix, string emotionKey) MakeKey(string characterId, string variantKey, string emotionKey)
    {
        characterId = (characterId ?? "").Trim();
        variantKey = (variantKey ?? "").Trim();
        emotionKey = NormalizeEmotionCode(emotionKey);

        char suffix = (variantKey.Length > 0) 
            ? variantKey[^1]
            : '\0';
        
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