using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PortraitResolver
{
    private const string DefaultVariant = "a";

    private readonly Dictionary<(string characterId, char variantSuffix, string emotionKey), Sprite> _map = new();

    public PortraitResolver(PortraitGeneratedDbSo db)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));
        if (db.entries == null) throw new InvalidOperationException("PortraitGeneratedDbSo.entries is null.");

        var entries = db.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];

            string characterId = (e.characterId ?? "").Trim();
            string variantKey = (e.variantKey ?? "").Trim();
            string emotionKey = NormalizeEmotionCode(e.emotionKey);

            if (string.IsNullOrEmpty(characterId) ||
                string.IsNullOrEmpty(variantKey) ||
                string.IsNullOrEmpty(emotionKey))
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[PortraitResolver] Invalid portrait entry ignored. " +
                    $"character='{e.characterId}', variant='{e.variantKey}', emotion='{e.emotionKey}'.");
#endif
                continue;
            }

            var key = MakeKey(characterId, variantKey, emotionKey);

            if (!_map.TryAdd(key, e.sprite))
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[PortraitResolver] Duplicate portrait key ignored. " +
                    $"{characterId}|{variantKey}|{emotionKey}");
#endif
            }
        }
    }

    public Sprite Resolve(
        CommandRunScope scope,
        string targetKey,
        PortraitIdentity portrait,
        string debugName)
    {
        string roleKey = CharacterRigTargetResolver.ResolveRoleKeyFromTargetKey(scope, targetKey);

        string character = portrait.character;
        string variant = portrait.variant;
        string emotion = portrait.emotion;

        ApplyCastBindingContextIfNeeded(scope, roleKey, ref character, ref variant);

        NormalizeAndValidateIdentity(
            targetKey,
            roleKey,
            debugName,
            ref character,
            ref variant,
            ref emotion);

        Sprite sprite = ResolveSpriteByKeys(character, variant, emotion);

        if (sprite == null)
        {
            string normalizedEmotion = NormalizeEmotionCode(emotion);

            throw new InvalidOperationException(
                $"[{debugName}] Failed to resolve portrait. " +
                $"targetKey='{targetKey}', roleKey='{roleKey}', " +
                $"character='{character}', variant='{variant}', " +
                $"emotion='{emotion}', normalizedEmotion='{normalizedEmotion}'.");
        }

        return sprite;
    }

    private static void ApplyCastBindingContextIfNeeded(
        CommandRunScope scope,
        string roleKey,
        ref string character,
        ref string variant)
    {
        if (!string.IsNullOrEmpty(character) && !string.IsNullOrEmpty(variant))
            return;

        ApplyCastBindingContext(scope, roleKey, ref character, ref variant);
    }

    private static void ApplyCastBindingContext(
        CommandRunScope scope,
        string roleKey,
        ref string character,
        ref string variant)
    {
        if (scope == null)
            return;

        if (scope.CastRegistry == null)
            return;

        if (!scope.CastRegistry.TryGetBinding(roleKey, out CastBinding binding))
            return;

        if (string.IsNullOrEmpty(character))
            character = binding.CharacterKey;

        if (string.IsNullOrEmpty(variant))
            variant = binding.VariantKey;
    }

    private static void NormalizeAndValidateIdentity(
        string targetKey,
        string roleKey,
        string debugName,
        ref string character,
        ref string variant,
        ref string emotion)
    {
        character = (character ?? "").Trim();
        variant = (variant ?? "").Trim();
        emotion = (emotion ?? "").Trim();

        if (string.IsNullOrEmpty(character))
        {
            throw new InvalidOperationException(
                $"[{debugName}] Character is empty. " +
                $"targetKey='{targetKey}', roleKey='{roleKey}'.");
        }

        if (string.IsNullOrEmpty(variant))
            variant = DefaultVariant;

        if (string.IsNullOrEmpty(emotion))
        {
            throw new InvalidOperationException(
                $"[{debugName}] Emotion is empty. " +
                $"targetKey='{targetKey}', roleKey='{roleKey}', " +
                $"character='{character}', variant='{variant}'.");
        }
    }

    private Sprite ResolveSpriteByKeys(string characterId, string variantKey, string emotionKey)
    {
        characterId = (characterId ?? "").Trim();
        variantKey = (variantKey ?? "").Trim();
        emotionKey = NormalizeEmotionCode(emotionKey);

        if (characterId.Length == 0 || variantKey.Length == 0 || emotionKey.Length == 0)
            return null;

        var key = MakeKey(characterId, variantKey, emotionKey);
        return _map.TryGetValue(key, out Sprite sprite) && sprite ? sprite : null;
    }

    private static (string characterId, char variantSuffix, string emotionKey)
        MakeKey(string characterId, string variantKey, string emotionKey)
    {
        characterId = (characterId ?? "").Trim();
        variantKey = (variantKey ?? "").Trim();
        emotionKey = (emotionKey ?? "").Trim();

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