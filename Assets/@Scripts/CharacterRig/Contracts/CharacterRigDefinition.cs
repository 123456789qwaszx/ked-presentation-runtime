using UnityEngine;
using UnityEngine.UI;

public static class CharacterRigSchema
{
    public enum Refs
    {
        // Slot axis - stage placement
        CharSlot_Anchor,
        CharSlot_DepthY,
        CharSlot_Track,
        CharSlot_Track_Focus,
        CharSlot_Track_Idle,
        CharSlot_Track_X,
        CharSlot_Track_Y,
        CharSlot_Rotation,
        CharSlot_SwayPivot,
        CharSlot_DepthScale,
        CharSlot_Scale,
        CharSlot_Size,
        
        // Character casting axis - per-character defaults
        Character_Root, 
        Character_CastTransform,
        
        // Portrait acting axis
        CharacterPortrait_Track,
        CharacterPortrait_Track_Move,
        CharacterPortrait_Track_X,
        CharacterPortrait_Track_Y,
        CharacterPortrait_Rotation,
        CharacterPortrait_SwayPivot,
        CharacterPortrait_Shake,

        // Portrait acting axis
        CharacterPortrait_ActingScale,
        CharacterPortrait_ActingScale_X,
        CharacterPortrait_ActingScale_Y,
        
        // Portrait sprite
        CharacterPortraitSprite_Root,
        CharacterPortraitSprite_Image,
        
        CharacterPortraitSpriteOverlay_Root,
        CharacterPortraitSpriteOverlay_Image,
        
        // Portrait extension / preserved systems
        Character_ExtensionsRoot,
        
        // Emoji00
        CharacterEmojiSlot00_Root,
        CharacterEmojiSlot00_CastTransform,

        EmojiSlot00_Track_Move,
        EmojiSlot00_Track_X,
        EmojiSlot00_Track_Y,
        CharacterEmojiSlot00_Effect,
        EmojiSlot00_BaseSize,
        EmojiSlot00_Scale,
        EmojiSlot00_SwayPivot,
        EmojiSlot00_BaseRotation,
        EmojiSlot00_Rotation,
        EmojiSlot00_Rotation_Offset,
        EmojiSlot00_Image,

        // Emoji01
        CharacterEmojiSlot01_Root,
        CharacterEmojiSlot01_CastTransform,

        EmojiSlot01_Track_Move,
        EmojiSlot01_Track_X,
        EmojiSlot01_Track_Y,
        CharacterEmojiSlot01_Effect,
        EmojiSlot01_BaseSize,
        EmojiSlot01_Scale,
        EmojiSlot01_SwayPivot,
        EmojiSlot01_BaseRotation,
        EmojiSlot01_Rotation,
        EmojiSlot01_Rotation_Offset,
        EmojiSlot01_Image,

        // Emoji02
        CharacterEmojiSlot02_Root,
        CharacterEmojiSlot02_CastTransform,

        EmojiSlot02_Track_Move,
        EmojiSlot02_Track_X,
        EmojiSlot02_Track_Y,
        CharacterEmojiSlot02_Effect,
        EmojiSlot02_BaseSize,
        EmojiSlot02_Scale,
        EmojiSlot02_SwayPivot,
        EmojiSlot02_BaseRotation,
        EmojiSlot02_Rotation,
        EmojiSlot02_Rotation_Offset,
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
        new() { Id = Refs.CharSlot_Anchor,    Parent = null },
        new() { Id = Refs.CharSlot_DepthY,    Parent = Refs.CharSlot_Anchor },
        new() { Id = Refs.CharSlot_Track,     Parent = Refs.CharSlot_DepthY },
        new() { Id = Refs.CharSlot_Track_Focus,   Parent = Refs.CharSlot_Track },
        new() { Id = Refs.CharSlot_Track_Idle,   Parent = Refs.CharSlot_Track_Focus },
        new() { Id = Refs.CharSlot_Track_X,   Parent = Refs.CharSlot_Track_Idle },
        new() { Id = Refs.CharSlot_Track_Y,   Parent = Refs.CharSlot_Track_X },
        new() { Id = Refs.CharSlot_Rotation,  Parent = Refs.CharSlot_Track_Y },
        new() { Id = Refs.CharSlot_SwayPivot, Parent = Refs.CharSlot_Rotation, NeedsBottomPivot = true},
        new() { Id = Refs.CharSlot_DepthScale, Parent = Refs.CharSlot_SwayPivot, NeedsBottomPivot = true},
        new() { Id = Refs.CharSlot_Scale,     Parent = Refs.CharSlot_DepthScale, NeedsBottomPivot = true},
        new() { Id = Refs.CharSlot_Size,      Parent = Refs.CharSlot_Scale,     NeedsBottomPivot = true },
    
        // Character casting axis - per-character defaults
        new() { Id = Refs.Character_Root,          Parent = Refs.CharSlot_Size, NeedsCanvasGroup = true },
        new() { Id = Refs.Character_CastTransform, Parent = Refs.Character_Root, NeedsBottomPivot = true },
    
        // Portrait acting axis
        new() { Id = Refs.CharacterPortrait_Track,      Parent = Refs.Character_CastTransform },
        new() { Id = Refs.CharacterPortrait_Track_Move, Parent = Refs.CharacterPortrait_Track },
        new() { Id = Refs.CharacterPortrait_Track_X,    Parent = Refs.CharacterPortrait_Track_Move },
        new() { Id = Refs.CharacterPortrait_Track_Y,    Parent = Refs.CharacterPortrait_Track_X },
        new() { Id = Refs.CharacterPortrait_Rotation,   Parent = Refs.CharacterPortrait_Track_Y },
        new() { Id = Refs.CharacterPortrait_SwayPivot,  Parent = Refs.CharacterPortrait_Rotation, NeedsBottomPivot = true },
        new() { Id = Refs.CharacterPortrait_Shake,      Parent = Refs.CharacterPortrait_SwayPivot },
        
        // Portrait acting axis
        new() { Id = Refs.CharacterPortrait_ActingScale,    Parent = Refs.CharacterPortrait_Shake },
        new() { Id = Refs.CharacterPortrait_ActingScale_X,  Parent = Refs.CharacterPortrait_ActingScale },
        new() { Id = Refs.CharacterPortrait_ActingScale_Y,  Parent = Refs.CharacterPortrait_ActingScale_X },
    
        // Portrait sprite
        new() { Id = Refs.CharacterPortraitSprite_Root,  Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f},
        new() { Id = Refs.CharacterPortraitSprite_Image, Parent = Refs.CharacterPortraitSprite_Root, NeedsImage = true },
    
        // Portrait sprite overlay
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Root,  Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Image, Parent = Refs.CharacterPortraitSpriteOverlay_Root, NeedsImage = true },
        
        // Portrait extension / preserved systems
        new() { Id = Refs.Character_ExtensionsRoot, Parent = Refs.CharacterPortrait_ActingScale_Y },
        
        // Emoji00 casting axis
        new() { Id = Refs.CharacterEmojiSlot00_Root,          Parent = Refs.CharacterPortrait_Shake, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterEmojiSlot00_CastTransform, Parent = Refs.CharacterEmojiSlot00_Root },
        
        // Emoji00 sprite motion / effect axis
        new() { Id = Refs.EmojiSlot00_Track_Move,        Parent = Refs.CharacterEmojiSlot00_CastTransform },
        new() { Id = Refs.EmojiSlot00_Track_X,           Parent = Refs.EmojiSlot00_Track_Move },
        new() { Id = Refs.EmojiSlot00_Track_Y,           Parent = Refs.EmojiSlot00_Track_X },
        new() { Id = Refs.CharacterEmojiSlot00_Effect,   Parent = Refs.EmojiSlot00_Track_Y },
        new() { Id = Refs.EmojiSlot00_BaseSize,          Parent = Refs.CharacterEmojiSlot00_Effect },
        new() { Id = Refs.EmojiSlot00_Scale,             Parent = Refs.EmojiSlot00_BaseSize },
        new() { Id = Refs.EmojiSlot00_SwayPivot,         Parent = Refs.EmojiSlot00_Scale, NeedsBottomPivot = true },
        new() { Id = Refs.EmojiSlot00_BaseRotation,      Parent = Refs.EmojiSlot00_SwayPivot },
        new() { Id = Refs.EmojiSlot00_Rotation,          Parent = Refs.EmojiSlot00_BaseRotation },
        new() { Id = Refs.EmojiSlot00_Rotation_Offset,   Parent = Refs.EmojiSlot00_Rotation },
        new() { Id = Refs.EmojiSlot00_Image,             Parent = Refs.EmojiSlot00_Rotation_Offset, NeedsImage = true },
        
        // Emoji01 casting axis
        new() { Id = Refs.CharacterEmojiSlot01_Root,          Parent = Refs.CharacterPortrait_Shake, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterEmojiSlot01_CastTransform, Parent = Refs.CharacterEmojiSlot01_Root },
        
        // Emoji01 sprite motion / effect axis
        new() { Id = Refs.EmojiSlot01_Track_Move,        Parent = Refs.CharacterEmojiSlot01_CastTransform },
        new() { Id = Refs.EmojiSlot01_Track_X,           Parent = Refs.EmojiSlot01_Track_Move },
        new() { Id = Refs.EmojiSlot01_Track_Y,           Parent = Refs.EmojiSlot01_Track_X },
        new() { Id = Refs.CharacterEmojiSlot01_Effect,   Parent = Refs.EmojiSlot01_Track_Y },
        new() { Id = Refs.EmojiSlot01_BaseSize,          Parent = Refs.CharacterEmojiSlot01_Effect },
        new() { Id = Refs.EmojiSlot01_Scale,             Parent = Refs.EmojiSlot01_BaseSize },
        new() { Id = Refs.EmojiSlot01_SwayPivot,         Parent = Refs.EmojiSlot01_Scale, NeedsBottomPivot = true },
        new() { Id = Refs.EmojiSlot01_BaseRotation,      Parent = Refs.EmojiSlot01_SwayPivot },
        new() { Id = Refs.EmojiSlot01_Rotation,          Parent = Refs.EmojiSlot01_BaseRotation },
        new() { Id = Refs.EmojiSlot01_Rotation_Offset,   Parent = Refs.EmojiSlot01_Rotation },
        new() { Id = Refs.EmojiSlot01_Image,             Parent = Refs.EmojiSlot01_Rotation_Offset, NeedsImage = true },
        
        // Emoji02 casting axis
        new() { Id = Refs.CharacterEmojiSlot02_Root,          Parent = Refs.CharacterPortrait_Shake, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterEmojiSlot02_CastTransform, Parent = Refs.CharacterEmojiSlot02_Root },
        
        // Emoji02 sprite motion / effect axis
        new() { Id = Refs.EmojiSlot02_Track_Move,        Parent = Refs.CharacterEmojiSlot02_CastTransform },
        new() { Id = Refs.EmojiSlot02_Track_X,           Parent = Refs.EmojiSlot02_Track_Move },
        new() { Id = Refs.EmojiSlot02_Track_Y,           Parent = Refs.EmojiSlot02_Track_X },
        new() { Id = Refs.CharacterEmojiSlot02_Effect,   Parent = Refs.EmojiSlot02_Track_Y },
        new() { Id = Refs.EmojiSlot02_BaseSize,          Parent = Refs.CharacterEmojiSlot02_Effect },
        new() { Id = Refs.EmojiSlot02_Scale,             Parent = Refs.EmojiSlot02_BaseSize },
        new() { Id = Refs.EmojiSlot02_SwayPivot,         Parent = Refs.EmojiSlot02_Scale, NeedsBottomPivot = true },
        new() { Id = Refs.EmojiSlot02_BaseRotation,      Parent = Refs.EmojiSlot02_SwayPivot },
        new() { Id = Refs.EmojiSlot02_Rotation,          Parent = Refs.EmojiSlot02_BaseRotation },
        new() { Id = Refs.EmojiSlot02_Rotation_Offset,   Parent = Refs.EmojiSlot02_Rotation },
        new() { Id = Refs.EmojiSlot02_Image,             Parent = Refs.EmojiSlot02_Rotation_Offset, NeedsImage = true },
    };
}

public enum CharacterRigTarget
{
    // Slot axis - stage placement
    CharSlot_Anchor,
    CharSlot_DepthY,
    CharSlot_Track,
    CharSlot_Track_Focus,
    CharSlot_Track_Idle,
    CharSlot_Track_X,
    CharSlot_Track_Y,
    CharSlot_Rotation,
    CharSlot_SwayPivot,
    CharSlot_DepthScale,
    CharSlot_Scale,
    CharSlot_Size,

    // Character casting axis - per-character defaults
    Character_Root,
    Character_CastTransform,

    // Portrait acting axis
    CharacterPortrait_Track,
    CharacterPortrait_Track_Move,
    CharacterPortrait_Track_X,
    CharacterPortrait_Track_Y,
    CharacterPortrait_Rotation,
    CharacterPortrait_SwayPivot,
    CharacterPortrait_Shake,
    
    // Portrait acting axis
    CharacterPortrait_ActingScale,
    CharacterPortrait_ActingScale_X,
    CharacterPortrait_ActingScale_Y,

    // Portrait sprite
    CharacterPortraitSprite_Root,
    CharacterPortraitSprite_Image,

    CharacterPortraitSpriteOverlay_Root,
    CharacterPortraitSpriteOverlay_Image,

    // Portrait extension / preserved systems
    Character_ExtensionsRoot,
    
    // Emoji00
    CharacterEmojiSlot00_Root,
    CharacterEmojiSlot00_CastTransform,

    EmojiSlot00_Track_Move,
    EmojiSlot00_Track_X,
    EmojiSlot00_Track_Y,
    CharacterEmojiSlot00_Effect,
    EmojiSlot00_BaseSize,
    EmojiSlot00_Scale,
    EmojiSlot00_SwayPivot,
    EmojiSlot00_BaseRotation,
    EmojiSlot00_Rotation,
    EmojiSlot00_Rotation_Offset,
    EmojiSlot00_Image,

    // Emoji01
    CharacterEmojiSlot01_Root,
    CharacterEmojiSlot01_CastTransform,

    EmojiSlot01_Track_Move,
    EmojiSlot01_Track_X,
    EmojiSlot01_Track_Y,
    CharacterEmojiSlot01_Effect,
    EmojiSlot01_BaseSize,
    EmojiSlot01_Scale,
    EmojiSlot01_SwayPivot,
    EmojiSlot01_BaseRotation,
    EmojiSlot01_Rotation,
    EmojiSlot01_Rotation_Offset,
    EmojiSlot01_Image,

    // Emoji02
    CharacterEmojiSlot02_Root,
    CharacterEmojiSlot02_CastTransform,

    EmojiSlot02_Track_Move,
    EmojiSlot02_Track_X,
    EmojiSlot02_Track_Y,
    CharacterEmojiSlot02_Effect,
    EmojiSlot02_BaseSize,
    EmojiSlot02_Scale,
    EmojiSlot02_SwayPivot,
    EmojiSlot02_BaseRotation,
    EmojiSlot02_Rotation,
    EmojiSlot02_Rotation_Offset,
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
    public RectTransform CharSlot_Anchor;
    public RectTransform CharSlot_DepthY;
    public RectTransform CharSlot_Track;
    public RectTransform CharSlot_Track_Focus;
    public RectTransform CharSlot_Track_Idle;
    public RectTransform CharSlot_Track_X;
    public RectTransform CharSlot_Track_Y;
    public RectTransform CharSlot_Rotation;
    public RectTransform CharSlot_SwayPivot;
    public RectTransform CharSlot_DepthScale;
    public RectTransform CharSlot_Scale;
    public RectTransform CharSlot_Size;

    // Character casting axis - per-character defaults
    public RectTransform Character_Root;
    public RectTransform Character_CastTransform;

    // Portrait acting axis
    public RectTransform CharacterPortrait_Track;
    public RectTransform CharacterPortrait_Track_Move;
    public RectTransform CharacterPortrait_Track_X;
    public RectTransform CharacterPortrait_Track_Y;
    public RectTransform CharacterPortrait_Rotation;
    public RectTransform CharacterPortrait_SwayPivot;
    public RectTransform CharacterPortrait_Shake;
    
    public RectTransform CharacterPortrait_ActingScale;
    public RectTransform CharacterPortrait_ActingScale_X;
    public RectTransform CharacterPortrait_ActingScale_Y;

    // Portrait sprite
    public RectTransform CharacterPortraitSprite_Root;
    public Image         CharacterPortraitSprite_Image;

    public RectTransform CharacterPortraitSpriteOverlay_Root;
    public Image         CharacterPortraitSpriteOverlay_Image;

    // Portrait extension / preserved systems
    public RectTransform Character_ExtensionsRoot;
    
    // Emoji00 casting/effect axis
    public RectTransform CharacterEmojiSlot00_Root;
    public RectTransform CharacterEmojiSlot00_CastTransform;

    // Emoji00 sprite motion / effect axis
    public RectTransform EmojiSlot00_Track_Move;
    public RectTransform EmojiSlot00_Track_X;
    public RectTransform EmojiSlot00_Track_Y;
    public RectTransform CharacterEmojiSlot00_Effect;
    public RectTransform EmojiSlot00_BaseSize;
    public RectTransform EmojiSlot00_Scale;
    public RectTransform EmojiSlot00_SwayPivot;
    public RectTransform EmojiSlot00_BaseRotation;
    public RectTransform EmojiSlot00_Rotation;
    public RectTransform EmojiSlot00_Rotation_Offset;
    public Image         EmojiSlot00_Image;

    // Emoji01 casting/effect axis
    public RectTransform CharacterEmojiSlot01_Root;
    public RectTransform CharacterEmojiSlot01_CastTransform;

    // Emoji01 sprite motion / effect axis
    public RectTransform EmojiSlot01_Track_Move;
    public RectTransform EmojiSlot01_Track_X;
    public RectTransform EmojiSlot01_Track_Y;
    public RectTransform CharacterEmojiSlot01_Effect;
    public RectTransform EmojiSlot01_BaseSize;
    public RectTransform EmojiSlot01_Scale;
    public RectTransform EmojiSlot01_SwayPivot;
    public RectTransform EmojiSlot01_BaseRotation;
    public RectTransform EmojiSlot01_Rotation;
    public RectTransform EmojiSlot01_Rotation_Offset;
    public Image         EmojiSlot01_Image;

    // Emoji02 casting/effect axis
    public RectTransform CharacterEmojiSlot02_Root;
    public RectTransform CharacterEmojiSlot02_CastTransform;

    // Emoji02 sprite motion / effect axis
    public RectTransform EmojiSlot02_Track_Move;
    public RectTransform EmojiSlot02_Track_X;
    public RectTransform EmojiSlot02_Track_Y;
    public RectTransform CharacterEmojiSlot02_Effect;
    public RectTransform EmojiSlot02_BaseSize;
    public RectTransform EmojiSlot02_Scale;
    public RectTransform EmojiSlot02_SwayPivot;
    public RectTransform EmojiSlot02_BaseRotation;
    public RectTransform EmojiSlot02_Rotation;
    public RectTransform EmojiSlot02_Rotation_Offset;
    public Image         EmojiSlot02_Image;
}

public static class CharacterRigRefsExtensions
{
    public static RectTransform GetRect(this CharacterRigRefs refs, CharacterRigTarget target)
    {
        return refs?.GetComponent(target).transform as RectTransform;
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
            // Slot axis - stage placement
            CharacterRigTarget.CharSlot_Anchor      => refs.CharSlot_Anchor,
            CharacterRigTarget.CharSlot_DepthY      => refs.CharSlot_DepthY,
            CharacterRigTarget.CharSlot_Track       => refs.CharSlot_Track,
            CharacterRigTarget.CharSlot_Track_Focus => refs.CharSlot_Track_Focus,
            CharacterRigTarget.CharSlot_Track_Idle => refs.CharSlot_Track_Idle,
            CharacterRigTarget.CharSlot_Track_X   => refs.CharSlot_Track_X,
            CharacterRigTarget.CharSlot_Track_Y   => refs.CharSlot_Track_Y,
            CharacterRigTarget.CharSlot_Rotation  => refs.CharSlot_Rotation,
            CharacterRigTarget.CharSlot_SwayPivot => refs.CharSlot_SwayPivot,
            CharacterRigTarget.CharSlot_DepthScale => refs.CharSlot_DepthScale,
            CharacterRigTarget.CharSlot_Scale     => refs.CharSlot_Scale,
            CharacterRigTarget.CharSlot_Size      => refs.CharSlot_Size,

            // Character casting axis - per-character defaults
            CharacterRigTarget.Character_Root          => refs.Character_Root,
            CharacterRigTarget.Character_CastTransform => refs.Character_CastTransform,

            // Portrait acting axis
            CharacterRigTarget.CharacterPortrait_Track          => refs.CharacterPortrait_Track,
            CharacterRigTarget.CharacterPortrait_Track_Move     => refs.CharacterPortrait_Track_Move,
            CharacterRigTarget.CharacterPortrait_Track_X        => refs.CharacterPortrait_Track_X,
            CharacterRigTarget.CharacterPortrait_Track_Y        => refs.CharacterPortrait_Track_Y,
            CharacterRigTarget.CharacterPortrait_Rotation       => refs.CharacterPortrait_Rotation,
            CharacterRigTarget.CharacterPortrait_SwayPivot      => refs.CharacterPortrait_SwayPivot,
            CharacterRigTarget.CharacterPortrait_Shake          => refs.CharacterPortrait_Shake,
            
            // Portrait sprite
            CharacterRigTarget.CharacterPortraitSprite_Root  => refs.CharacterPortraitSprite_Root,
            CharacterRigTarget.CharacterPortraitSprite_Image => refs.CharacterPortraitSprite_Image,

            CharacterRigTarget.CharacterPortraitSpriteOverlay_Root  => refs.CharacterPortraitSpriteOverlay_Root,
            CharacterRigTarget.CharacterPortraitSpriteOverlay_Image => refs.CharacterPortraitSpriteOverlay_Image,

            // Portrait extension / preserved systems
            CharacterRigTarget.Character_ExtensionsRoot => refs.Character_ExtensionsRoot,
            
            // Emoji00 casting/effect axis
            CharacterRigTarget.CharacterEmojiSlot00_Root          => refs.CharacterEmojiSlot00_Root,
            CharacterRigTarget.CharacterEmojiSlot00_CastTransform => refs.CharacterEmojiSlot00_CastTransform,
            
            // Emoji00 sprite motion / effect axis
            CharacterRigTarget.EmojiSlot00_Track_Move       => refs.EmojiSlot00_Track_Move,
            CharacterRigTarget.EmojiSlot00_Track_X          => refs.EmojiSlot00_Track_X,
            CharacterRigTarget.EmojiSlot00_Track_Y          => refs.EmojiSlot00_Track_Y,
            CharacterRigTarget.CharacterEmojiSlot00_Effect  => refs.CharacterEmojiSlot00_Effect,
            CharacterRigTarget.EmojiSlot00_BaseSize         => refs.EmojiSlot00_BaseSize,
            CharacterRigTarget.EmojiSlot00_Scale            => refs.EmojiSlot00_Scale,
            CharacterRigTarget.EmojiSlot00_SwayPivot        => refs.EmojiSlot00_SwayPivot,
            CharacterRigTarget.EmojiSlot00_BaseRotation     => refs.EmojiSlot00_BaseRotation,
            CharacterRigTarget.EmojiSlot00_Rotation         => refs.EmojiSlot00_Rotation,
            CharacterRigTarget.EmojiSlot00_Rotation_Offset  => refs.EmojiSlot00_Rotation_Offset,
            CharacterRigTarget.EmojiSlot00_Image            => refs.EmojiSlot00_Image,
            
            // Emoji01 casting/effect axis
            CharacterRigTarget.CharacterEmojiSlot01_Root          => refs.CharacterEmojiSlot01_Root,
            CharacterRigTarget.CharacterEmojiSlot01_CastTransform => refs.CharacterEmojiSlot01_CastTransform,
            
            // Emoji01 sprite motion / effect axis
            CharacterRigTarget.EmojiSlot01_Track_Move       => refs.EmojiSlot01_Track_Move,
            CharacterRigTarget.EmojiSlot01_Track_X          => refs.EmojiSlot01_Track_X,
            CharacterRigTarget.EmojiSlot01_Track_Y          => refs.EmojiSlot01_Track_Y,
            CharacterRigTarget.CharacterEmojiSlot01_Effect  => refs.CharacterEmojiSlot01_Effect,
            CharacterRigTarget.EmojiSlot01_BaseSize         => refs.EmojiSlot01_BaseSize,
            CharacterRigTarget.EmojiSlot01_Scale            => refs.EmojiSlot01_Scale,
            CharacterRigTarget.EmojiSlot01_SwayPivot        => refs.EmojiSlot01_SwayPivot,
            CharacterRigTarget.EmojiSlot01_BaseRotation     => refs.EmojiSlot01_BaseRotation,
            CharacterRigTarget.EmojiSlot01_Rotation         => refs.EmojiSlot01_Rotation,
            CharacterRigTarget.EmojiSlot01_Rotation_Offset  => refs.EmojiSlot01_Rotation_Offset,
            CharacterRigTarget.EmojiSlot01_Image            => refs.EmojiSlot01_Image,
            
            // Emoji02 casting/effect axis
            CharacterRigTarget.CharacterEmojiSlot02_Root          => refs.CharacterEmojiSlot02_Root,
            CharacterRigTarget.CharacterEmojiSlot02_CastTransform => refs.CharacterEmojiSlot02_CastTransform,
            
            // Emoji02 sprite motion / effect axis
            CharacterRigTarget.EmojiSlot02_Track_Move       => refs.EmojiSlot02_Track_Move,
            CharacterRigTarget.EmojiSlot02_Track_X          => refs.EmojiSlot02_Track_X,
            CharacterRigTarget.EmojiSlot02_Track_Y          => refs.EmojiSlot02_Track_Y,
            CharacterRigTarget.CharacterEmojiSlot02_Effect  => refs.CharacterEmojiSlot02_Effect,
            CharacterRigTarget.EmojiSlot02_BaseSize         => refs.EmojiSlot02_BaseSize,
            CharacterRigTarget.EmojiSlot02_Scale            => refs.EmojiSlot02_Scale,
            CharacterRigTarget.EmojiSlot02_SwayPivot        => refs.EmojiSlot02_SwayPivot,
            CharacterRigTarget.EmojiSlot02_BaseRotation     => refs.EmojiSlot02_BaseRotation,
            CharacterRigTarget.EmojiSlot02_Rotation         => refs.EmojiSlot02_Rotation,
            CharacterRigTarget.EmojiSlot02_Rotation_Offset  => refs.EmojiSlot02_Rotation_Offset,
            CharacterRigTarget.EmojiSlot02_Image            => refs.EmojiSlot02_Image,
            
            CharacterRigTarget.CharacterPortrait_ActingScale    => refs.CharacterPortrait_ActingScale,
            CharacterRigTarget.CharacterPortrait_ActingScale_X  => refs.CharacterPortrait_ActingScale_X,
            CharacterRigTarget.CharacterPortrait_ActingScale_Y  => refs.CharacterPortrait_ActingScale_Y,


            _ => null
        };
    }
}