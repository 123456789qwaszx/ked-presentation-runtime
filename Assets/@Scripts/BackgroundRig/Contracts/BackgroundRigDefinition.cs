using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class BackgroundRigSchema
{
    public enum Refs
    {
        // Framing axis - pseudo camera / focus response
        Background_FramingTransform,
        Background_FramingScale,

        // Background casting axis - per-background defaults
        Background_Root,
        Background_CastTransform,

        // Background acting axis
        Background_Track,
        Background_Track_Move,
        Background_Track_X,
        Background_Track_Y,
        Background_Rotation,
        Background_Shake,
        Background_ActingScale,
        Background_ActingScale_X,
        Background_ActingScale_Y,

        // Layer stack
        Background_LayerRoot,

        // Back layer
        Background_BackLayer_Root,
        Background_BackLayer_Image,

        // Object slots
        Background_ObjectSlotRoot,
        Background_ObjectSlot00,
        Background_ObjectSlot01,
        Background_ObjectSlot02,

        // Front layer
        Background_FrontLayer_Root,
        Background_FrontLayer_Image,

        // Extension / preserved systems
        Background_ExtensionsRoot
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;

        public bool NeedsImage;
        public bool NeedsCanvasGroup;
        public bool NeedsCenterPivot;
        public bool NeedsBottomPivot;

        public float InitialCanvasGroupAlpha = 1f;
    }

    public static readonly NodeDef[] Nodes =
    {
        // Framing axis - pseudo camera / focus response
        new() { Id = Refs.Background_FramingTransform, Parent = null },
        new() { Id = Refs.Background_FramingScale, Parent = Refs.Background_FramingTransform },

        // Background casting axis - per-background defaults
        new() { Id = Refs.Background_Root, Parent = Refs.Background_FramingScale, NeedsCanvasGroup = true },
        new() { Id = Refs.Background_CastTransform, Parent = Refs.Background_Root },

        // Background acting axis
        new() { Id = Refs.Background_Track, Parent = Refs.Background_CastTransform },
        new() { Id = Refs.Background_Track_Move, Parent = Refs.Background_Track },
        new() { Id = Refs.Background_Track_X, Parent = Refs.Background_Track_Move },
        new() { Id = Refs.Background_Track_Y, Parent = Refs.Background_Track_X },
        new() { Id = Refs.Background_Rotation, Parent = Refs.Background_Track_Y },
        new() { Id = Refs.Background_Shake, Parent = Refs.Background_Rotation },
        new() { Id = Refs.Background_ActingScale, Parent = Refs.Background_Shake },
        new() { Id = Refs.Background_ActingScale_X, Parent = Refs.Background_ActingScale },
        new() { Id = Refs.Background_ActingScale_Y, Parent = Refs.Background_ActingScale_X },

        // Layer stack
        new() { Id = Refs.Background_LayerRoot, Parent = Refs.Background_ActingScale_Y },

        // Back layer
        new() { Id = Refs.Background_BackLayer_Root, Parent = Refs.Background_LayerRoot, NeedsCanvasGroup = true },
        new() { Id = Refs.Background_BackLayer_Image, Parent = Refs.Background_BackLayer_Root, NeedsImage = true },

        // Object slots
        new() { Id = Refs.Background_ObjectSlotRoot, Parent = Refs.Background_LayerRoot },
        new() { Id = Refs.Background_ObjectSlot00, Parent = Refs.Background_ObjectSlotRoot },
        new() { Id = Refs.Background_ObjectSlot01, Parent = Refs.Background_ObjectSlotRoot },
        new() { Id = Refs.Background_ObjectSlot02, Parent = Refs.Background_ObjectSlotRoot },

        // Front layer
        new() { Id = Refs.Background_FrontLayer_Root, Parent = Refs.Background_LayerRoot, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.Background_FrontLayer_Image, Parent = Refs.Background_FrontLayer_Root, NeedsImage = true },

        // Extension / preserved systems
        new() { Id = Refs.Background_ExtensionsRoot, Parent = Refs.Background_LayerRoot },
    };
}

public enum BackgroundRigTarget
{
    // Framing axis - pseudo camera / focus response
    Background_FramingTransform,
    Background_FramingScale,

    // Background casting axis - per-background defaults
    Background_Root,
    Background_CastTransform,

    // Background acting axis
    Background_Track,
    Background_Track_Move,
    Background_Track_X,
    Background_Track_Y,
    Background_Rotation,
    Background_Shake,
    Background_ActingScale,
    Background_ActingScale_X,
    Background_ActingScale_Y,

    // Layer stack
    Background_LayerRoot,

    // Back layer
    Background_BackLayer_Root,
    Background_BackLayer_Image,

    // Object slots
    Background_ObjectSlotRoot,
    Background_ObjectSlot00,
    Background_ObjectSlot01,
    Background_ObjectSlot02,

    // Front layer
    Background_FrontLayer_Root,
    Background_FrontLayer_Image,

    // Extension / preserved systems
    Background_ExtensionsRoot
}

public sealed class BackgroundRigRefs
{
    public RectTransform RigRoot { get; private set; }

    public BackgroundRigRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }

    // Framing axis - pseudo camera / focus response
    public RectTransform Background_FramingTransform;
    public RectTransform Background_FramingScale;

    // Background casting axis - per-background defaults
    public RectTransform Background_Root;
    public RectTransform Background_CastTransform;

    // Background acting axis
    public RectTransform Background_Track;
    public RectTransform Background_Track_Move;
    public RectTransform Background_Track_X;
    public RectTransform Background_Track_Y;
    public RectTransform Background_Rotation;
    public RectTransform Background_Shake;
    public RectTransform Background_ActingScale;
    public RectTransform Background_ActingScale_X;
    public RectTransform Background_ActingScale_Y;

    // Layer stack
    public RectTransform Background_LayerRoot;

    // Back layer
    public RectTransform Background_BackLayer_Root;
    public Image Background_BackLayer_Image;

    // Object slots
    public RectTransform Background_ObjectSlotRoot;
    public RectTransform Background_ObjectSlot00;
    public RectTransform Background_ObjectSlot01;
    public RectTransform Background_ObjectSlot02;

    // Front layer
    public RectTransform Background_FrontLayer_Root;
    public Image Background_FrontLayer_Image;

    // Extension / preserved systems
    public RectTransform Background_ExtensionsRoot;
}

public static class BackgroundRigRefsExtensions
{
    public static RectTransform GetRect(this BackgroundRigRefs refs, BackgroundRigTarget target)
    {
        Component c = refs.GetComponent(target);

        if (c == null)
            return null;

        return c.transform as RectTransform;
    }

    public static Image GetImage(this BackgroundRigRefs refs, BackgroundRigTarget target)
    {
        return refs.GetComponent(target) as Image;
    }

    private static Component GetComponent(this BackgroundRigRefs refs, BackgroundRigTarget target)
    {
        if (refs == null)
            return null;

        return target switch
        {
            // Framing axis - pseudo camera / focus response
            BackgroundRigTarget.Background_FramingTransform => refs.Background_FramingTransform,
            BackgroundRigTarget.Background_FramingScale => refs.Background_FramingScale,

            // Background casting axis - per-background defaults
            BackgroundRigTarget.Background_Root => refs.Background_Root,
            BackgroundRigTarget.Background_CastTransform => refs.Background_CastTransform,

            // Background acting axis
            BackgroundRigTarget.Background_Track => refs.Background_Track,
            BackgroundRigTarget.Background_Track_Move => refs.Background_Track_Move,
            BackgroundRigTarget.Background_Track_X => refs.Background_Track_X,
            BackgroundRigTarget.Background_Track_Y => refs.Background_Track_Y,
            BackgroundRigTarget.Background_Rotation => refs.Background_Rotation,
            BackgroundRigTarget.Background_Shake => refs.Background_Shake,
            BackgroundRigTarget.Background_ActingScale => refs.Background_ActingScale,
            BackgroundRigTarget.Background_ActingScale_X => refs.Background_ActingScale_X,
            BackgroundRigTarget.Background_ActingScale_Y => refs.Background_ActingScale_Y,

            // Layer stack
            BackgroundRigTarget.Background_LayerRoot => refs.Background_LayerRoot,

            // Back layer
            BackgroundRigTarget.Background_BackLayer_Root => refs.Background_BackLayer_Root,
            BackgroundRigTarget.Background_BackLayer_Image => refs.Background_BackLayer_Image,

            // Object slots
            BackgroundRigTarget.Background_ObjectSlotRoot => refs.Background_ObjectSlotRoot,
            BackgroundRigTarget.Background_ObjectSlot00 => refs.Background_ObjectSlot00,
            BackgroundRigTarget.Background_ObjectSlot01 => refs.Background_ObjectSlot01,
            BackgroundRigTarget.Background_ObjectSlot02 => refs.Background_ObjectSlot02,

            // Front layer
            BackgroundRigTarget.Background_FrontLayer_Root => refs.Background_FrontLayer_Root,
            BackgroundRigTarget.Background_FrontLayer_Image => refs.Background_FrontLayer_Image,

            // Extension / preserved systems
            BackgroundRigTarget.Background_ExtensionsRoot => refs.Background_ExtensionsRoot,

            _ => null
        };
    }
}