using System.Collections.Generic;
using UnityEngine;

public sealed class PortraitResolver
{
    public const string DefaultVariant = "a";
    public const string DefaultEmotion = "01";
    public const string FallbackEmotion = "01";

    private readonly Dictionary<(string characterId, char variantSuffix, string emotionKey), Sprite> _map = new();

    public PortraitResolver(PortraitGeneratedDBSO db)
    {
        BuildPortraitMap(db);
    }
    
    private void BuildPortraitMap(PortraitGeneratedDBSO db)
    {
        var entries = db.entries;
        
        for (int i = 0; i < entries.Count; i++)
        {
            PortraitGeneratedDBSO.Entry e = entries[i];
            
            var key = MakeKey(e.characterId, e.variantKey, e.emotionKey);

            if (!_map.TryAdd(key, e.sprite))
                Debug.LogWarning($"[PortraitResolver] Duplicate portrait key ignored. index={i}, key='{key}'");
        }
    }

    public Sprite Resolve(CommandRunScope scope, string targetKey, PortraitIdentity portrait, string debugName)
    {
        string roleKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, targetKey);

        string character = portrait.character;
        string variant = portrait.variant;
        string emotion = portrait.emotion;
    
        if (string.IsNullOrEmpty(character))
        {
            if (scope.CastRegistry.TryGetCharacter(roleKey, out string characterKey))
                character = characterKey;
        }

        if (string.IsNullOrEmpty(variant))
        {
            if (scope.CastRegistry.TryGetVariant(roleKey, out string variantKey))
                variant = variantKey;
        }
    
        if (string.IsNullOrEmpty(variant))
            variant = DefaultVariant;
    
        if (string.IsNullOrEmpty(emotion))
            emotion = DefaultEmotion;

        character = PresentationKeyNormalizer.NormalizeCharacterKey(character);
        variant = PresentationKeyNormalizer.NormalizeVariantKey(variant);

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

    private static (string characterId, char variantSuffix, string emotionKey) MakeKey(
        string characterId,
        string variantKey,
        string emotionKey)
    {
        characterId = PresentationKeyNormalizer.NormalizeCharacterKey(characterId);
        char suffix = PresentationKeyNormalizer.NormalizeVariantSuffix(variantKey);
        emotionKey = NormalizeEmotionCode(emotionKey);

        return (characterId, suffix, emotionKey);
    }

    // Normalizes the input into a 2-digit code in the form "02".
    // - "2"  -> "02"
    // - "02" -> "02"
    // - otherwise -> "" (unsupported)
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