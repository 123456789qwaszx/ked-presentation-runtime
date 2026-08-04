using UnityEngine;
using UnityEngine.UI;

public static class CharacterRigSchema
{
    public enum Refs
    {
        // Slot axis - stage placement
        CharSlot_Track_Focus,
        CharSlot_DepthY,
        CharSlot_DepthScale,
        CharSlot_Track_Idle,
        CharSlot_Track,
        CharSlot_Track_X,
        CharSlot_Track_Y,
        CharSlot_Rotation,
        CharSlot_SwayPivot,
        CharSlot_Scale,

        // Character casting axis - per-character defaults
        CharacterPortrait_VisualOffset,

        // Portrait acting axis
        CharacterPortrait_Track,
        CharacterPortrait_Rotation,
        CharacterPortrait_Track_Move,
        CharacterPortrait_Track_Move_X,
        CharacterPortrait_Track_Move_Y,
        CharacterPortrait_SwayPivot,
        CharacterPortrait_Shake,
        CharacterPortrait_ActingScale,
        CharacterPortrait_ActingScale_X,
        CharacterPortrait_ActingScale_Y,

        // Portrait sprite
        CharacterPortraitSprite_Root,
        CharacterPortraitSprite_Image,

        // Portrait sprite overlay
        CharacterPortraitSpriteOverlay_Root,
        CharacterPortraitSpriteOverlay_Image,

        // Portrait extension / preserved systems
        Character_ExtensionsRoot,

        // Emoji00
        EmojiSlot00_Root,
        EmojiSlot00_VisualOffset,

        // Emoji00 sprite motion / effect axis
        EmojiSlot00_Track,
        EmojiSlot00_BaseRotation,
        EmojiSlot00_Track_Move,
        EmojiSlot00_Track_Move_X,
        EmojiSlot00_Track_Move_Y,
        EmojiSlot00_Effect,
        EmojiSlot00_SwayPivot,
        EmojiSlot00_Rotation,
        EmojiSlot00_Size,
        EmojiSlot00_Scale,
        EmojiSlot00_Image,

        // Emoji01
        EmojiSlot01_Root,
        EmojiSlot01_VisualOffset,

        // Emoji01 sprite motion / effect axis
        EmojiSlot01_Track,
        EmojiSlot01_BaseRotation,
        EmojiSlot01_Track_Move,
        EmojiSlot01_Track_Move_X,
        EmojiSlot01_Track_Move_Y,
        EmojiSlot01_Effect,
        EmojiSlot01_SwayPivot,
        EmojiSlot01_Rotation,
        EmojiSlot01_Size,
        EmojiSlot01_Scale,
        EmojiSlot01_Image,

        // Emoji02
        EmojiSlot02_Root,
        EmojiSlot02_VisualOffset,

        // Emoji02 sprite motion / effect axis
        EmojiSlot02_Track,
        EmojiSlot02_BaseRotation,
        EmojiSlot02_Track_Move,
        EmojiSlot02_Track_Move_X,
        EmojiSlot02_Track_Move_Y,
        EmojiSlot02_Effect,
        EmojiSlot02_SwayPivot,
        EmojiSlot02_Rotation,
        EmojiSlot02_Size,
        EmojiSlot02_Scale,
        EmojiSlot02_Image,
    }

    public sealed class NodeDef
    {
        public Refs  Id;
        public Refs? Parent;

        public bool  NeedsImage;
        public bool  NeedsCanvasGroup;
        public bool  NeedsBottomPivot;

        public float InitialCanvasGroupAlpha = 1f;
    }

    public static readonly NodeDef[] Nodes =
    {
        // Slot axis - stage placement
        new() { Id = Refs.CharSlot_Track_Focus, Parent = null },
        new() { Id = Refs.CharSlot_DepthY, Parent = Refs.CharSlot_Track_Focus },
        new() { Id = Refs.CharSlot_DepthScale, Parent = Refs.CharSlot_DepthY, NeedsBottomPivot = true },
        new() { Id = Refs.CharSlot_Track_Idle, Parent = Refs.CharSlot_DepthScale },
        new() { Id = Refs.CharSlot_Track, Parent = Refs.CharSlot_Track_Idle },
        new() { Id = Refs.CharSlot_Track_X, Parent = Refs.CharSlot_Track },
        new() { Id = Refs.CharSlot_Track_Y, Parent = Refs.CharSlot_Track_X },
        new() { Id = Refs.CharSlot_Rotation, Parent = Refs.CharSlot_Track_Y },
        new() { Id = Refs.CharSlot_SwayPivot, Parent = Refs.CharSlot_Rotation, NeedsBottomPivot = true },
        new() { Id = Refs.CharSlot_Scale, Parent = Refs.CharSlot_SwayPivot, NeedsBottomPivot = true },

        // Character casting axis - per-character defaults
        new() { Id = Refs.CharacterPortrait_VisualOffset, Parent = Refs.CharSlot_Scale, NeedsBottomPivot = true},

        // Portrait acting axis
        new() { Id = Refs.CharacterPortrait_Track, Parent = Refs.CharacterPortrait_VisualOffset },
        new() { Id = Refs.CharacterPortrait_Rotation, Parent = Refs.CharacterPortrait_Track },
        new() { Id = Refs.CharacterPortrait_Track_Move, Parent = Refs.CharacterPortrait_Rotation },
        new() { Id = Refs.CharacterPortrait_Track_Move_X, Parent = Refs.CharacterPortrait_Track_Move },
        new() { Id = Refs.CharacterPortrait_Track_Move_Y, Parent = Refs.CharacterPortrait_Track_Move_X },
        new() { Id = Refs.CharacterPortrait_SwayPivot, Parent = Refs.CharacterPortrait_Track_Move_Y, NeedsBottomPivot = true },
        new() { Id = Refs.CharacterPortrait_Shake, Parent = Refs.CharacterPortrait_SwayPivot },
        new() { Id = Refs.CharacterPortrait_ActingScale, Parent = Refs.CharacterPortrait_Shake },
        new() { Id = Refs.CharacterPortrait_ActingScale_X, Parent = Refs.CharacterPortrait_ActingScale },
        new() { Id = Refs.CharacterPortrait_ActingScale_Y, Parent = Refs.CharacterPortrait_ActingScale_X },

        // Portrait sprite
        new() { Id = Refs.CharacterPortraitSprite_Root, Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSprite_Image, Parent = Refs.CharacterPortraitSprite_Root, NeedsImage = true },

        // Portrait sprite overlay
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Root, Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Image, Parent = Refs.CharacterPortraitSpriteOverlay_Root, NeedsImage = true },

        // Portrait extension / preserved systems
        new() { Id = Refs.Character_ExtensionsRoot, Parent = Refs.CharacterPortrait_ActingScale_Y },

        // Emoji00
        new() { Id = Refs.EmojiSlot00_Root, Parent = Refs.CharacterPortrait_Shake, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.EmojiSlot00_VisualOffset, Parent = Refs.EmojiSlot00_Root , NeedsBottomPivot = true},

        // Emoji00 sprite motion / effect axis
        new() { Id = Refs.EmojiSlot00_Track, Parent = Refs.EmojiSlot00_VisualOffset },
        new() { Id = Refs.EmojiSlot00_BaseRotation, Parent = Refs.EmojiSlot00_Track },
        new() { Id = Refs.EmojiSlot00_Track_Move, Parent = Refs.EmojiSlot00_BaseRotation },
        new() { Id = Refs.EmojiSlot00_Track_Move_X, Parent = Refs.EmojiSlot00_Track_Move },
        new() { Id = Refs.EmojiSlot00_Track_Move_Y, Parent = Refs.EmojiSlot00_Track_Move_X },
        new() { Id = Refs.EmojiSlot00_Effect, Parent = Refs.EmojiSlot00_Track_Move_Y },
        new() { Id = Refs.EmojiSlot00_SwayPivot, Parent = Refs.EmojiSlot00_Effect, NeedsBottomPivot = true },
        new() { Id = Refs.EmojiSlot00_Rotation, Parent = Refs.EmojiSlot00_SwayPivot },
        new() { Id = Refs.EmojiSlot00_Size, Parent = Refs.EmojiSlot00_Rotation },
        new() { Id = Refs.EmojiSlot00_Scale, Parent = Refs.EmojiSlot00_Size },
        new() { Id = Refs.EmojiSlot00_Image, Parent = Refs.EmojiSlot00_Scale, NeedsImage = true },

        // Emoji01
        new() { Id = Refs.EmojiSlot01_Root, Parent = Refs.CharacterPortrait_Shake, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.EmojiSlot01_VisualOffset, Parent = Refs.EmojiSlot01_Root, NeedsBottomPivot = true },

        // Emoji01 sprite motion / effect axis
        new() { Id = Refs.EmojiSlot01_Track, Parent = Refs.EmojiSlot01_VisualOffset },
        new() { Id = Refs.EmojiSlot01_BaseRotation, Parent = Refs.EmojiSlot01_Track },
        new() { Id = Refs.EmojiSlot01_Track_Move, Parent = Refs.EmojiSlot01_BaseRotation },
        new() { Id = Refs.EmojiSlot01_Track_Move_X, Parent = Refs.EmojiSlot01_Track_Move },
        new() { Id = Refs.EmojiSlot01_Track_Move_Y, Parent = Refs.EmojiSlot01_Track_Move_X },
        new() { Id = Refs.EmojiSlot01_Effect, Parent = Refs.EmojiSlot01_Track_Move_Y },
        new() { Id = Refs.EmojiSlot01_SwayPivot, Parent = Refs.EmojiSlot01_Effect, NeedsBottomPivot = true },
        new() { Id = Refs.EmojiSlot01_Rotation, Parent = Refs.EmojiSlot01_SwayPivot },
        new() { Id = Refs.EmojiSlot01_Size, Parent = Refs.EmojiSlot01_Rotation },
        new() { Id = Refs.EmojiSlot01_Scale, Parent = Refs.EmojiSlot01_Size },
        new() { Id = Refs.EmojiSlot01_Image, Parent = Refs.EmojiSlot01_Scale, NeedsImage = true },

        // Emoji02
        new() { Id = Refs.EmojiSlot02_Root, Parent = Refs.CharacterPortrait_Shake, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.EmojiSlot02_VisualOffset, Parent = Refs.EmojiSlot02_Root, NeedsBottomPivot = true },

        // Emoji02 sprite motion / effect axis
        new() { Id = Refs.EmojiSlot02_Track, Parent = Refs.EmojiSlot02_VisualOffset },
        new() { Id = Refs.EmojiSlot02_BaseRotation, Parent = Refs.EmojiSlot02_Track },
        new() { Id = Refs.EmojiSlot02_Track_Move, Parent = Refs.EmojiSlot02_BaseRotation },
        new() { Id = Refs.EmojiSlot02_Track_Move_X, Parent = Refs.EmojiSlot02_Track_Move },
        new() { Id = Refs.EmojiSlot02_Track_Move_Y, Parent = Refs.EmojiSlot02_Track_Move_X },
        new() { Id = Refs.EmojiSlot02_Effect, Parent = Refs.EmojiSlot02_Track_Move_Y },
        new() { Id = Refs.EmojiSlot02_SwayPivot, Parent = Refs.EmojiSlot02_Effect, NeedsBottomPivot = true },
        new() { Id = Refs.EmojiSlot02_Rotation, Parent = Refs.EmojiSlot02_SwayPivot },
        new() { Id = Refs.EmojiSlot02_Size, Parent = Refs.EmojiSlot02_Rotation },
        new() { Id = Refs.EmojiSlot02_Scale, Parent = Refs.EmojiSlot02_Size },
        new() { Id = Refs.EmojiSlot02_Image, Parent = Refs.EmojiSlot02_Scale, NeedsImage = true },
    };
}

public enum CharacterRigTarget
{
    RigRoot,
    
    // Slot axis - stage placement
    CharSlot_Track_Focus,
    CharSlot_DepthY,
    CharSlot_DepthScale,
    CharSlot_Track_Idle,
    CharSlot_Track,
    CharSlot_Track_X,
    CharSlot_Track_Y,
    CharSlot_Rotation,
    CharSlot_SwayPivot,
    CharSlot_Scale,

    // Character casting axis - per-character defaults
    CharacterPortrait_VisualOffset,

    // Portrait acting axis
    CharacterPortrait_Track,
    CharacterPortrait_Rotation,
    CharacterPortrait_Track_Move,
    CharacterPortrait_Track_Move_X,
    CharacterPortrait_Track_Move_Y,
    CharacterPortrait_SwayPivot,
    CharacterPortrait_Shake,
    CharacterPortrait_ActingScale,
    CharacterPortrait_ActingScale_X,
    CharacterPortrait_ActingScale_Y,

    // Portrait sprite
    CharacterPortraitSprite_Root,
    CharacterPortraitSprite_Image,

    // Portrait sprite overlay
    CharacterPortraitSpriteOverlay_Root,
    CharacterPortraitSpriteOverlay_Image,

    // Portrait extension / preserved systems
    Character_ExtensionsRoot,

    // Emoji00
    EmojiSlot00_Root,
    EmojiSlot00_VisualOffset,

    // Emoji00 sprite motion / effect axis
    EmojiSlot00_Track,
    EmojiSlot00_BaseRotation,
    EmojiSlot00_Track_Move,
    EmojiSlot00_Track_Move_X,
    EmojiSlot00_Track_Move_Y,
    EmojiSlot00_Effect,
    EmojiSlot00_SwayPivot,
    EmojiSlot00_Rotation,
    EmojiSlot00_Size,
    EmojiSlot00_Scale,
    EmojiSlot00_Image,

    // Emoji01
    EmojiSlot01_Root,
    EmojiSlot01_VisualOffset,

    // Emoji01 sprite motion / effect axis
    EmojiSlot01_Track,
    EmojiSlot01_BaseRotation,
    EmojiSlot01_Track_Move,
    EmojiSlot01_Track_Move_X,
    EmojiSlot01_Track_Move_Y,
    EmojiSlot01_Effect,
    EmojiSlot01_SwayPivot,
    EmojiSlot01_Rotation,
    EmojiSlot01_Size,
    EmojiSlot01_Scale,
    EmojiSlot01_Image,

    // Emoji02
    EmojiSlot02_Root,
    EmojiSlot02_VisualOffset,

    // Emoji02 sprite motion / effect axis
    EmojiSlot02_Track,
    EmojiSlot02_BaseRotation,
    EmojiSlot02_Track_Move,
    EmojiSlot02_Track_Move_X,
    EmojiSlot02_Track_Move_Y,
    EmojiSlot02_Effect,
    EmojiSlot02_SwayPivot,
    EmojiSlot02_Rotation,
    EmojiSlot02_Size,
    EmojiSlot02_Scale,
    EmojiSlot02_Image,
}

public sealed class CharacterRigRefs
{
    public RectTransform RigRoot { get; private set; }
    public CharacterRigRefs(RectTransform rigRoot) => RigRoot = rigRoot;

    // Visual effect: CharacterPortraitSprite_Image에 바인딩된 runtime material 소유자.
    // SetupCharRigCommand가 생성, CharacterRigRegistry.DestroyRig가 Dispose.
    public CharacterRigVisualEffectController VisualEffect;

    public CharacterPlacementTargetLedger PlacementTargets { get; } = new();

    // Slot axis - stage placement
    public RectTransform CharSlot_Track_Focus;
    public RectTransform CharSlot_DepthY;
    public RectTransform CharSlot_DepthScale;
    public RectTransform CharSlot_Track_Idle;
    public RectTransform CharSlot_Track;
    public RectTransform CharSlot_Track_X;
    public RectTransform CharSlot_Track_Y;
    public RectTransform CharSlot_Rotation;
    public RectTransform CharSlot_SwayPivot;
    public RectTransform CharSlot_Scale;

    // Character casting axis - per-character defaults
    public RectTransform CharacterPortrait_VisualOffset;

    // Portrait acting axis
    public RectTransform CharacterPortrait_Track;
    public RectTransform CharacterPortrait_Rotation;
    public RectTransform CharacterPortrait_Track_Move;
    public RectTransform CharacterPortrait_Track_Move_X;
    public RectTransform CharacterPortrait_Track_Move_Y;
    public RectTransform CharacterPortrait_SwayPivot;
    public RectTransform CharacterPortrait_Shake;
    public RectTransform CharacterPortrait_ActingScale;
    public RectTransform CharacterPortrait_ActingScale_X;
    public RectTransform CharacterPortrait_ActingScale_Y;

    // Portrait sprite
    public RectTransform CharacterPortraitSprite_Root;
    public Image         CharacterPortraitSprite_Image;

    // Portrait sprite overlay
    public RectTransform CharacterPortraitSpriteOverlay_Root;
    public Image         CharacterPortraitSpriteOverlay_Image;

    // Portrait extension / preserved systems
    public RectTransform Character_ExtensionsRoot;

    // Emoji00
    public RectTransform EmojiSlot00_Root;
    public RectTransform EmojiSlot00_VisualOffset;

    // Emoji00 sprite motion / effect axis
    public RectTransform EmojiSlot00_Track;
    public RectTransform EmojiSlot00_BaseRotation;
    public RectTransform EmojiSlot00_Track_Move;
    public RectTransform EmojiSlot00_Track_Move_X;
    public RectTransform EmojiSlot00_Track_Move_Y;
    public RectTransform EmojiSlot00_Effect;
    public RectTransform EmojiSlot00_SwayPivot;
    public RectTransform EmojiSlot00_Rotation;
    public RectTransform EmojiSlot00_Size;
    public RectTransform EmojiSlot00_Scale;
    public Image         EmojiSlot00_Image;

    // Emoji01
    public RectTransform EmojiSlot01_Root;
    public RectTransform EmojiSlot01_VisualOffset;

    // Emoji01 sprite motion / effect axis
    public RectTransform EmojiSlot01_Track;
    public RectTransform EmojiSlot01_BaseRotation;
    public RectTransform EmojiSlot01_Track_Move;
    public RectTransform EmojiSlot01_Track_Move_X;
    public RectTransform EmojiSlot01_Track_Move_Y;
    public RectTransform EmojiSlot01_Effect;
    public RectTransform EmojiSlot01_SwayPivot;
    public RectTransform EmojiSlot01_Rotation;
    public RectTransform EmojiSlot01_Size;
    public RectTransform EmojiSlot01_Scale;
    public Image         EmojiSlot01_Image;

    // Emoji02
    public RectTransform EmojiSlot02_Root;
    public RectTransform EmojiSlot02_VisualOffset;

    // Emoji02 sprite motion / effect axis
    public RectTransform EmojiSlot02_Track;
    public RectTransform EmojiSlot02_BaseRotation;
    public RectTransform EmojiSlot02_Track_Move;
    public RectTransform EmojiSlot02_Track_Move_X;
    public RectTransform EmojiSlot02_Track_Move_Y;
    public RectTransform EmojiSlot02_Effect;
    public RectTransform EmojiSlot02_SwayPivot;
    public RectTransform EmojiSlot02_Rotation;
    public RectTransform EmojiSlot02_Size;
    public RectTransform EmojiSlot02_Scale;
    public Image         EmojiSlot02_Image;
}

public static class CharacterRigRefsExtensions
{
    public static RectTransform GetRect(this CharacterRigRefs refs, CharacterRigTarget target)
    {
        Component component = refs?.GetComponent(target);
        return component != null ? component.transform as RectTransform : null;
    }

    public static Image GetImage(this CharacterRigRefs refs, CharacterRigTarget target)
    {
        return refs?.GetComponent(target) as Image;
    }

    private static Component GetComponent(this CharacterRigRefs refs, CharacterRigTarget target)
    {
        if (refs == null)
            return null;

        return target switch
        {
            CharacterRigTarget.RigRoot => refs.RigRoot,
            
            // Slot axis - stage placement
            CharacterRigTarget.CharSlot_Track_Focus => refs.CharSlot_Track_Focus,
            CharacterRigTarget.CharSlot_DepthY => refs.CharSlot_DepthY,
            CharacterRigTarget.CharSlot_DepthScale => refs.CharSlot_DepthScale,
            CharacterRigTarget.CharSlot_Track_Idle => refs.CharSlot_Track_Idle,
            CharacterRigTarget.CharSlot_Track => refs.CharSlot_Track,
            CharacterRigTarget.CharSlot_Track_X => refs.CharSlot_Track_X,
            CharacterRigTarget.CharSlot_Track_Y => refs.CharSlot_Track_Y,
            CharacterRigTarget.CharSlot_Rotation => refs.CharSlot_Rotation,
            CharacterRigTarget.CharSlot_SwayPivot => refs.CharSlot_SwayPivot,
            CharacterRigTarget.CharSlot_Scale => refs.CharSlot_Scale,

            // Character casting axis - per-character defaults
            CharacterRigTarget.CharacterPortrait_VisualOffset => refs.CharacterPortrait_VisualOffset,

            // Portrait acting axis
            CharacterRigTarget.CharacterPortrait_Track => refs.CharacterPortrait_Track,
            CharacterRigTarget.CharacterPortrait_Rotation => refs.CharacterPortrait_Rotation,
            CharacterRigTarget.CharacterPortrait_Track_Move => refs.CharacterPortrait_Track_Move,
            CharacterRigTarget.CharacterPortrait_Track_Move_X => refs.CharacterPortrait_Track_Move_X,
            CharacterRigTarget.CharacterPortrait_Track_Move_Y => refs.CharacterPortrait_Track_Move_Y,
            CharacterRigTarget.CharacterPortrait_SwayPivot => refs.CharacterPortrait_SwayPivot,
            CharacterRigTarget.CharacterPortrait_Shake => refs.CharacterPortrait_Shake,
            CharacterRigTarget.CharacterPortrait_ActingScale => refs.CharacterPortrait_ActingScale,
            CharacterRigTarget.CharacterPortrait_ActingScale_X => refs.CharacterPortrait_ActingScale_X,
            CharacterRigTarget.CharacterPortrait_ActingScale_Y => refs.CharacterPortrait_ActingScale_Y,

            // Portrait sprite
            CharacterRigTarget.CharacterPortraitSprite_Root => refs.CharacterPortraitSprite_Root,
            CharacterRigTarget.CharacterPortraitSprite_Image => refs.CharacterPortraitSprite_Image,

            // Portrait sprite overlay
            CharacterRigTarget.CharacterPortraitSpriteOverlay_Root => refs.CharacterPortraitSpriteOverlay_Root,
            CharacterRigTarget.CharacterPortraitSpriteOverlay_Image => refs.CharacterPortraitSpriteOverlay_Image,

            // Portrait extension / preserved systems
            CharacterRigTarget.Character_ExtensionsRoot => refs.Character_ExtensionsRoot,

            // Emoji00
            CharacterRigTarget.EmojiSlot00_Root => refs.EmojiSlot00_Root,
            CharacterRigTarget.EmojiSlot00_VisualOffset => refs.EmojiSlot00_VisualOffset,

            // Emoji00 sprite motion / effect axis
            CharacterRigTarget.EmojiSlot00_Track => refs.EmojiSlot00_Track,
            CharacterRigTarget.EmojiSlot00_BaseRotation => refs.EmojiSlot00_BaseRotation,
            CharacterRigTarget.EmojiSlot00_Track_Move => refs.EmojiSlot00_Track_Move,
            CharacterRigTarget.EmojiSlot00_Track_Move_X => refs.EmojiSlot00_Track_Move_X,
            CharacterRigTarget.EmojiSlot00_Track_Move_Y => refs.EmojiSlot00_Track_Move_Y,
            CharacterRigTarget.EmojiSlot00_Effect => refs.EmojiSlot00_Effect,
            CharacterRigTarget.EmojiSlot00_SwayPivot => refs.EmojiSlot00_SwayPivot,
            CharacterRigTarget.EmojiSlot00_Rotation => refs.EmojiSlot00_Rotation,
            CharacterRigTarget.EmojiSlot00_Size => refs.EmojiSlot00_Size,
            CharacterRigTarget.EmojiSlot00_Scale => refs.EmojiSlot00_Scale,
            CharacterRigTarget.EmojiSlot00_Image => refs.EmojiSlot00_Image,

            // Emoji01
            CharacterRigTarget.EmojiSlot01_Root => refs.EmojiSlot01_Root,
            CharacterRigTarget.EmojiSlot01_VisualOffset => refs.EmojiSlot01_VisualOffset,

            // Emoji01 sprite motion / effect axis
            CharacterRigTarget.EmojiSlot01_Track => refs.EmojiSlot01_Track,
            CharacterRigTarget.EmojiSlot01_BaseRotation => refs.EmojiSlot01_BaseRotation,
            CharacterRigTarget.EmojiSlot01_Track_Move => refs.EmojiSlot01_Track_Move,
            CharacterRigTarget.EmojiSlot01_Track_Move_X => refs.EmojiSlot01_Track_Move_X,
            CharacterRigTarget.EmojiSlot01_Track_Move_Y => refs.EmojiSlot01_Track_Move_Y,
            CharacterRigTarget.EmojiSlot01_Effect => refs.EmojiSlot01_Effect,
            CharacterRigTarget.EmojiSlot01_SwayPivot => refs.EmojiSlot01_SwayPivot,
            CharacterRigTarget.EmojiSlot01_Rotation => refs.EmojiSlot01_Rotation,
            CharacterRigTarget.EmojiSlot01_Size => refs.EmojiSlot01_Size,
            CharacterRigTarget.EmojiSlot01_Scale => refs.EmojiSlot01_Scale,
            CharacterRigTarget.EmojiSlot01_Image => refs.EmojiSlot01_Image,

            // Emoji02
            CharacterRigTarget.EmojiSlot02_Root => refs.EmojiSlot02_Root,
            CharacterRigTarget.EmojiSlot02_VisualOffset => refs.EmojiSlot02_VisualOffset,

            // Emoji02 sprite motion / effect axis
            CharacterRigTarget.EmojiSlot02_Track => refs.EmojiSlot02_Track,
            CharacterRigTarget.EmojiSlot02_BaseRotation => refs.EmojiSlot02_BaseRotation,
            CharacterRigTarget.EmojiSlot02_Track_Move => refs.EmojiSlot02_Track_Move,
            CharacterRigTarget.EmojiSlot02_Track_Move_X => refs.EmojiSlot02_Track_Move_X,
            CharacterRigTarget.EmojiSlot02_Track_Move_Y => refs.EmojiSlot02_Track_Move_Y,
            CharacterRigTarget.EmojiSlot02_Effect => refs.EmojiSlot02_Effect,
            CharacterRigTarget.EmojiSlot02_SwayPivot => refs.EmojiSlot02_SwayPivot,
            CharacterRigTarget.EmojiSlot02_Rotation => refs.EmojiSlot02_Rotation,
            CharacterRigTarget.EmojiSlot02_Size => refs.EmojiSlot02_Size,
            CharacterRigTarget.EmojiSlot02_Scale => refs.EmojiSlot02_Scale,
            CharacterRigTarget.EmojiSlot02_Image => refs.EmojiSlot02_Image,

            _ => null
        };
    }
}