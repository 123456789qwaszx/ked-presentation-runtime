using System;
using UnityEngine;

public static class PortraitIdentityResolveUtility
{
    public static Sprite ResolveSprite(
        CommandRunScope scope,
        PortraitResolver resolver,
        string targetKey,
        PortraitIdentity portrait,
        string debugName)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (resolver == null)
            throw new InvalidOperationException($"[{debugName}] PortraitResolver is null.");

        if (portrait == null)
            throw new InvalidOperationException($"[{debugName}] PortraitIdentity is null.");

        string roleKey =
            CharacterRigTargetResolver.ResolveRoleKeyFromTargetKey(
                scope,
                targetKey);

        string character = SafeTrim(portrait.character);
        string variant = SafeTrim(portrait.variant);
        string emotion = SafeTrim(portrait.emotion);

        if (string.IsNullOrEmpty(character) || string.IsNullOrEmpty(variant))
        {
            TryFillFromCastBinding(
                scope,
                roleKey,
                ref character,
                ref variant);
        }

        if (string.IsNullOrEmpty(character))
        {
            throw new InvalidOperationException(
                $"[{debugName}] Character is empty. targetKey='{targetKey}', roleKey='{roleKey}'.");
        }

        if (string.IsNullOrEmpty(variant))
            variant = "a";

        if (string.IsNullOrEmpty(emotion))
        {
            throw new InvalidOperationException(
                $"[{debugName}] Emotion is empty. targetKey='{targetKey}', roleKey='{roleKey}', character='{character}', variant='{variant}'.");
        }

        Sprite sprite = resolver.Resolve(character, variant, emotion);

        if (sprite == null)
        {
            string normalizedEmotion =
                PortraitResolver.NormalizeEmotionCode(emotion);

            throw new InvalidOperationException(
                $"[{debugName}] Failed to resolve portrait. " +
                $"targetKey='{targetKey}', roleKey='{roleKey}', " +
                $"character='{character}', variant='{variant}', emotion='{emotion}', normalizedEmotion='{normalizedEmotion}'.");
        }

        return sprite;
    }

    private static void TryFillFromCastBinding(
        CommandRunScope scope,
        string roleKey,
        ref string character,
        ref string variant)
    {
        if (scope.CastRegistry == null)
            return;

        if (!scope.CastRegistry.TryGetBinding(roleKey, out CastBinding binding))
            return;

        if (string.IsNullOrEmpty(character))
            character = SafeTrim(binding.CharacterKey);

        if (string.IsNullOrEmpty(variant))
            variant = SafeTrim(binding.VariantKey);
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim();
    }
}