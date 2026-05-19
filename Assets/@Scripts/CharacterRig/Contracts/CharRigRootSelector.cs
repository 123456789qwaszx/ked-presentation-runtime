using UnityEngine;
using System;
using System.Collections.Generic;

[Flags]
public enum CharRigRootMask
{
    None = 0,
    CharacterPortrait_Root        = 1 << 0,
    CharacterPortraitOverlay_Root = 1 << 1,
    CharacterEmoji_Root           = 1 << 2,
}

public static class CharRigRootSelector
{
    private static readonly (CharRigRootMask flag, Func<CharacterRigRefs, RectTransform> get)[] Map =
    {
        (CharRigRootMask.CharacterPortrait_Root,        r => r.CharacterPortraitSprite_Root),
        (CharRigRootMask.CharacterPortraitOverlay_Root, r => r.CharacterPortraitSpriteOverlay_Root),
        (CharRigRootMask.CharacterEmoji_Root,           r => r.CharacterEmojiSlot00_Root),
    };

    public static void CollectRootRects(CharacterRigRefs refs, CharRigRootMask mask, List<RectTransform> outRects)
    {
        if (outRects == null)
        {
            Debug.LogError(
                "[CharRigRootMaskResolver] outRects is null. " +
                "Create a reusable List<RectTransform> in the Command, e.g. " +
                "'private readonly List<RectTransform> _rootRects = new();', " +
                "then pass it to CollectRects().");

            throw new ArgumentNullException(nameof(outRects));
        }
        
        outRects.Clear();

        if (refs == null || mask == CharRigRootMask.None)
            return;

        for (int i = 0; i < Map.Length; i++)
        {
            var (flag, getter) = Map[i];
            if ((mask & flag) == 0)
                continue;

            RectTransform rect = getter(refs);
            if (rect != null)
                outRects.Add(rect);
        }
    }
}