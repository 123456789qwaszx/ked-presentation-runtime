using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum BackgroundRigRootMask
{
    None = 0,

    Background_Root           = 1 << 0,
    BackgroundSprite_Root      = 1 << 1,
    Background_ObjectSlotRoot = 1 << 2,
    
    All =
        Background_Root |
        BackgroundSprite_Root |
        Background_ObjectSlotRoot
}

public static class BackgroundRigRootSelector
{
    private static readonly (BackgroundRigRootMask flag, Func<BackgroundRigRefs, RectTransform> get)[] Map =
    {
        (BackgroundRigRootMask.Background_Root,            r => r.Background_Root),
        (BackgroundRigRootMask.BackgroundSprite_Root,  r => r.BackgroundSprite_Root),
        (BackgroundRigRootMask.Background_ObjectSlotRoot,  r => r.Background_ObjectSlotRoot),
    };

    public static void CollectRects(
        BackgroundRigRefs refs,
        BackgroundRigRootMask mask,
        List<RectTransform> outRects)
    {
        if (outRects == null)
        {
            Debug.LogError(
                "[BackgroundRigRootSelector] outRects is null. " +
                "Create a reusable List<RectTransform> in the Command, e.g. " +
                "'private readonly List<RectTransform> _rootRects = new();', " +
                "then pass it to CollectRects().");

            throw new ArgumentNullException(nameof(outRects));
        }

        outRects.Clear();

        if (refs == null || mask == BackgroundRigRootMask.None)
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