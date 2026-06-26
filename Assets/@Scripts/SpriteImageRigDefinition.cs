using UnityEngine;
using UnityEngine.UI;

public static class SpriteImageRigSchema
{
    public enum Refs
    {
        Sprite_Root,

        Sprite_Anchor,

        // BaseRotation is above movement axis.
        // Track_X / Track_Y move along this rotated basis.
        Sprite_BaseRotation,

        Sprite_Track_Move,
        Sprite_Track_X,
        Sprite_Track_X_Offset,
        Sprite_Track_Y,
        Sprite_Track_Y_Offset,

        // Final visual rotation.
        // This does not affect Track_X / Track_Y movement direction.
        Sprite_Rotation,

        Sprite_Size,
        Sprite_Scale,

        Sprite_ActingScale,
        Sprite_ActingScale_X,
        Sprite_ActingScale_Y,

        Sprite_Image,
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;

        public bool NeedsImage;
        public bool NeedsCanvasGroup;

        public bool StretchFull;
        public bool NeedsCenterPivot = true;
        public bool NeedsBottomPivot;

        public float InitialCanvasGroupAlpha = 1f;
        public Color InitialImageColor = Color.white;
        public bool RaycastTarget = false;
    }

    public static readonly NodeDef[] Nodes =
    {
        new()
        {
            Id = Refs.Sprite_Root,
            Parent = null,
            StretchFull = true,
            NeedsCanvasGroup = true,
            InitialCanvasGroupAlpha = 0f,
        },

        new() { Id = Refs.Sprite_Anchor, Parent = Refs.Sprite_Root },

        new() { Id = Refs.Sprite_BaseRotation, Parent = Refs.Sprite_Anchor },

        new() { Id = Refs.Sprite_Track_Move, Parent = Refs.Sprite_BaseRotation },
        new() { Id = Refs.Sprite_Track_X, Parent = Refs.Sprite_Track_Move },
        new() { Id = Refs.Sprite_Track_X_Offset, Parent = Refs.Sprite_Track_X },
        new() { Id = Refs.Sprite_Track_Y, Parent = Refs.Sprite_Track_X_Offset },
        new() { Id = Refs.Sprite_Track_Y_Offset, Parent = Refs.Sprite_Track_Y },

        new() { Id = Refs.Sprite_Rotation, Parent = Refs.Sprite_Track_Y_Offset },

        new() { Id = Refs.Sprite_Size, Parent = Refs.Sprite_Rotation },
        new() { Id = Refs.Sprite_Scale, Parent = Refs.Sprite_Size },

        new() { Id = Refs.Sprite_ActingScale, Parent = Refs.Sprite_Scale },
        new() { Id = Refs.Sprite_ActingScale_X, Parent = Refs.Sprite_ActingScale },
        new() { Id = Refs.Sprite_ActingScale_Y, Parent = Refs.Sprite_ActingScale_X },

        new()
        {
            Id = Refs.Sprite_Image,
            Parent = Refs.Sprite_ActingScale_Y,
            StretchFull = true,
            NeedsImage = true,
            InitialImageColor = Color.white,
            RaycastTarget = false,
        },
    };
}

public enum SpriteImageRigTarget
{
    Sprite_Root,

    Sprite_Anchor,

    Sprite_BaseRotation,

    Sprite_Track_Move,
    Sprite_Track_X,
    Sprite_Track_X_Offset,
    Sprite_Track_Y,
    Sprite_Track_Y_Offset,

    Sprite_Rotation,

    Sprite_Size,
    Sprite_Scale,

    Sprite_ActingScale,
    Sprite_ActingScale_X,
    Sprite_ActingScale_Y,

    Sprite_Image,
}

public sealed class SpriteImageRigRefs
{
    public RectTransform RigRoot { get; }

    public CanvasGroup Sprite_RootCanvasGroup;
    public RectTransform Sprite_Root;

    public RectTransform Sprite_Anchor;

    public RectTransform Sprite_BaseRotation;

    public RectTransform Sprite_Track_Move;
    public RectTransform Sprite_Track_X;
    public RectTransform Sprite_Track_X_Offset;
    public RectTransform Sprite_Track_Y;
    public RectTransform Sprite_Track_Y_Offset;

    public RectTransform Sprite_Rotation;

    public RectTransform Sprite_Size;
    public RectTransform Sprite_Scale;

    public RectTransform Sprite_ActingScale;
    public RectTransform Sprite_ActingScale_X;
    public RectTransform Sprite_ActingScale_Y;

    public Image Sprite_Image;

    public SpriteImageRigRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }
}

public static class SpriteImageRigRefsExtensions
{
    public static RectTransform GetRect(
        this SpriteImageRigRefs refs,
        SpriteImageRigTarget target)
    {
        Component component = refs.GetComponent(target);
        return component != null ? component.transform as RectTransform : null;
    }

    public static Image GetImage(
        this SpriteImageRigRefs refs,
        SpriteImageRigTarget target)
    {
        return refs.GetComponent(target) as Image;
    }

    private static Component GetComponent(
        this SpriteImageRigRefs refs,
        SpriteImageRigTarget target)
    {
        if (refs == null)
            return null;

        return target switch
        {
            SpriteImageRigTarget.Sprite_Root => refs.Sprite_Root,

            SpriteImageRigTarget.Sprite_Anchor => refs.Sprite_Anchor,

            SpriteImageRigTarget.Sprite_BaseRotation => refs.Sprite_BaseRotation,

            SpriteImageRigTarget.Sprite_Track_Move => refs.Sprite_Track_Move,
            SpriteImageRigTarget.Sprite_Track_X => refs.Sprite_Track_X,
            SpriteImageRigTarget.Sprite_Track_X_Offset => refs.Sprite_Track_X_Offset,
            SpriteImageRigTarget.Sprite_Track_Y => refs.Sprite_Track_Y,
            SpriteImageRigTarget.Sprite_Track_Y_Offset => refs.Sprite_Track_Y_Offset,

            SpriteImageRigTarget.Sprite_Rotation => refs.Sprite_Rotation,

            SpriteImageRigTarget.Sprite_Size => refs.Sprite_Size,
            SpriteImageRigTarget.Sprite_Scale => refs.Sprite_Scale,

            SpriteImageRigTarget.Sprite_ActingScale => refs.Sprite_ActingScale,
            SpriteImageRigTarget.Sprite_ActingScale_X => refs.Sprite_ActingScale_X,
            SpriteImageRigTarget.Sprite_ActingScale_Y => refs.Sprite_ActingScale_Y,

            SpriteImageRigTarget.Sprite_Image => refs.Sprite_Image,

            _ => null,
        };
    }
}