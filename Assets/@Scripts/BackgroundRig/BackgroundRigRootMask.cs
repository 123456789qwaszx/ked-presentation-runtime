using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum BackgroundRigRootMask
{
    None = 0,

    Background_Root           = 1 << 0,
    Background_LayerRoot      = 1 << 1,

    Background_BackLayer_Root = 1 << 2,
    Background_ObjectSlotRoot = 1 << 3,
    Background_FrontLayer_Root = 1 << 4,

    Background_ExtensionsRoot = 1 << 5,

    VisualLayers =
        Background_BackLayer_Root |
        Background_ObjectSlotRoot |
        Background_FrontLayer_Root,

    All =
        Background_Root |
        Background_LayerRoot |
        Background_BackLayer_Root |
        Background_ObjectSlotRoot |
        Background_FrontLayer_Root |
        Background_ExtensionsRoot,
}

public static class BackgroundRigRootSelector
{
    private static readonly (BackgroundRigRootMask flag, Func<BackgroundRigRefs, RectTransform> get)[] Map =
    {
        (BackgroundRigRootMask.Background_Root,            r => r.Background_Root),
        (BackgroundRigRootMask.Background_LayerRoot,       r => r.Background_LayerRoot),
        (BackgroundRigRootMask.Background_BackLayer_Root,  r => r.Background_BackLayer_Root),
        (BackgroundRigRootMask.Background_ObjectSlotRoot,  r => r.Background_ObjectSlotRoot),
        (BackgroundRigRootMask.Background_FrontLayer_Root, r => r.Background_FrontLayer_Root),
        (BackgroundRigRootMask.Background_ExtensionsRoot,  r => r.Background_ExtensionsRoot),
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