using UnityEngine;
using System;
using System.Collections.Generic;

[Flags]
public enum CharRigRootLayerMask
{
    None = 0,
    CharacterPortrait_Root        = 1 << 0,
    CharacterPortraitOverlay_Root = 1 << 1,
    CharacterEmoji_Root           = 1 << 2,
}

public static class CharRigRootLayerMaskMap
{
    private static readonly (CharRigRootLayerMask flag, Func<CharacterRigRefs, RectTransform> get)[] Map =
    {
        (CharRigRootLayerMask.CharacterPortrait_Root,        r => r.CharacterPortrait_Root),
        (CharRigRootLayerMask.CharacterPortraitOverlay_Root, r => r.CharacterPortraitOverlay_Root),
        (CharRigRootLayerMask.CharacterEmoji_Root,           r => r.CharacterEmoji_Root),
    };

    public static void CollectRects(CharacterRigRefs refs, CharRigRootLayerMask mask, List<RectTransform> outRects)
    {
        outRects.Clear();

        if (refs == null || mask == CharRigRootLayerMask.None)
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