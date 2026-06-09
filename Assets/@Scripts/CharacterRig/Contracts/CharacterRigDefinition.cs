using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterRigSchema
{
    public enum Refs
    {
        // Slot axis - stage placement
        CharSlot_Anchor,
        CharSlot_Track,
        CharSlot_Track_X,
        CharSlot_Track_Y,
        CharSlot_Rotation,
        CharSlot_SwayPivot,
        CharSlot_Scale,
        
        // Framing axis - pseudo camera / focus response
        CharSlot_FramingTransform,
        CharSlot_FramingScale,
        CharSlot_FramingScale_X,
        CharSlot_FramingScale_Y,
        
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
        
        // Emoji00 casting/effect axis
        CharacterEmojiSlot00_Root,
        CharacterEmojiSlot00_CastTransform,
        CharacterEmojiSlot00_Effect,
        
        // Emoji00 sprite motion axis
        EmojiSlot00_Track,
        EmojiSlot00_Track_Move,
        EmojiSlot00_Track_X,
        EmojiSlot00_Track_Y,
        EmojiSlot00_Scale,
        EmojiSlot00_Rotation,
        EmojiSlot00_Image,
        
        // Emoji01
        CharacterEmojiSlot01_Root,
        CharacterEmojiSlot01_CastTransform,
        CharacterEmojiSlot01_Effect,
        
        EmojiSlot01_Track,
        EmojiSlot01_Track_Move,
        EmojiSlot01_Track_X,
        EmojiSlot01_Track_Y,
        EmojiSlot01_Scale,
        EmojiSlot01_Rotation,
        EmojiSlot01_Image,
        
        // Emoji02
        CharacterEmojiSlot02_Root,
        CharacterEmojiSlot02_CastTransform,
        CharacterEmojiSlot02_Effect,
        
        EmojiSlot02_Track,
        EmojiSlot02_Track_Move,
        EmojiSlot02_Track_X,
        EmojiSlot02_Track_Y,
        EmojiSlot02_Scale,
        EmojiSlot02_Rotation,
        EmojiSlot02_Image
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
        new() { Id = Refs.CharSlot_Anchor,      Parent = null },
        new() { Id = Refs.CharSlot_Track,       Parent = Refs.CharSlot_Anchor },
        new() { Id = Refs.CharSlot_Track_X,     Parent = Refs.CharSlot_Track },
        new() { Id = Refs.CharSlot_Track_Y,     Parent = Refs.CharSlot_Track_X },
        new() { Id = Refs.CharSlot_Rotation,    Parent = Refs.CharSlot_Track_Y },
        new() { Id = Refs.CharSlot_SwayPivot,    Parent = Refs.CharSlot_Rotation },
        new() { Id = Refs.CharSlot_Scale,       Parent = Refs.CharSlot_SwayPivot, NeedsBottomPivot = true },
    
        // Framing axis - pseudo camera / focus response
        new() { Id = Refs.CharSlot_FramingTransform, Parent = Refs.CharSlot_Scale },
        new() { Id = Refs.CharSlot_FramingScale,     Parent = Refs.CharSlot_FramingTransform },
        new() { Id = Refs.CharSlot_FramingScale_X,   Parent = Refs.CharSlot_FramingScale },
        new() { Id = Refs.CharSlot_FramingScale_Y,   Parent = Refs.CharSlot_FramingScale_X },
    
        // Character casting axis - per-character defaults
        new() { Id = Refs.Character_Root,          Parent = Refs.CharSlot_FramingScale_Y, NeedsCanvasGroup = true },
        new() { Id = Refs.Character_CastTransform, Parent = Refs.Character_Root, NeedsBottomPivot = true },
    
        // Portrait acting axis
        new() { Id = Refs.CharacterPortrait_Track,          Parent = Refs.Character_CastTransform,},
        new() { Id = Refs.CharacterPortrait_Track_Move,     Parent = Refs.CharacterPortrait_Track },
        new() { Id = Refs.CharacterPortrait_Track_X,        Parent = Refs.CharacterPortrait_Track_Move },
        new() { Id = Refs.CharacterPortrait_Track_Y,        Parent = Refs.CharacterPortrait_Track_X },
        new() { Id = Refs.CharacterPortrait_Rotation,       Parent = Refs.CharacterPortrait_Track_Y },
        new() { Id = Refs.CharacterPortrait_SwayPivot,      Parent = Refs.CharacterPortrait_Rotation, NeedsBottomPivot = true },
        new() { Id = Refs.CharacterPortrait_Shake,          Parent = Refs.CharacterPortrait_SwayPivot },
        new() { Id = Refs.CharacterPortrait_ActingScale,    Parent = Refs.CharacterPortrait_Shake },
        new() { Id = Refs.CharacterPortrait_ActingScale_X,  Parent = Refs.CharacterPortrait_ActingScale },
        new() { Id = Refs.CharacterPortrait_ActingScale_Y,  Parent = Refs.CharacterPortrait_ActingScale_X },
    
        // Portrait sprite
        new() { Id = Refs.CharacterPortraitSprite_Root,  Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true },
        new() { Id = Refs.CharacterPortraitSprite_Image, Parent = Refs.CharacterPortraitSprite_Root, NeedsImage = true },
    
        // Portrait sprite overlay
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Root,  Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Image, Parent = Refs.CharacterPortraitSpriteOverlay_Root, NeedsImage = true },
    
        // Portrait extension / preserved systems
        new() { Id = Refs.Character_ExtensionsRoot, Parent = Refs.CharacterPortrait_ActingScale_Y },
    
        // Emoji00 casting/effect axis
        new() { Id = Refs.CharacterEmojiSlot00_Root,          Parent = Refs.Character_CastTransform, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterEmojiSlot00_CastTransform, Parent = Refs.CharacterEmojiSlot00_Root },
        new() { Id = Refs.CharacterEmojiSlot00_Effect,        Parent = Refs.CharacterEmojiSlot00_CastTransform },
    
        // Emoji00 sprite motion axis
        new() { Id = Refs.EmojiSlot00_Track,      Parent = Refs.CharacterEmojiSlot00_Effect },
        new() { Id = Refs.EmojiSlot00_Track_Move, Parent = Refs.EmojiSlot00_Track },
        new() { Id = Refs.EmojiSlot00_Track_X,    Parent = Refs.EmojiSlot00_Track_Move },
        new() { Id = Refs.EmojiSlot00_Track_Y,    Parent = Refs.EmojiSlot00_Track_X },
        new() { Id = Refs.EmojiSlot00_Scale,      Parent = Refs.EmojiSlot00_Track_Y },
        new() { Id = Refs.EmojiSlot00_Rotation,   Parent = Refs.EmojiSlot00_Scale },
        new() { Id = Refs.EmojiSlot00_Image,      Parent = Refs.EmojiSlot00_Rotation, NeedsImage = true },
    
        // Emoji01 casting/effect axis
        new() { Id = Refs.CharacterEmojiSlot01_Root,          Parent = Refs.Character_CastTransform, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterEmojiSlot01_CastTransform, Parent = Refs.CharacterEmojiSlot01_Root },
        new() { Id = Refs.CharacterEmojiSlot01_Effect,        Parent = Refs.CharacterEmojiSlot01_CastTransform },
    
        // Emoji01 sprite motion axis
        new() { Id = Refs.EmojiSlot01_Track,      Parent = Refs.CharacterEmojiSlot01_Effect },
        new() { Id = Refs.EmojiSlot01_Track_Move, Parent = Refs.EmojiSlot01_Track },
        new() { Id = Refs.EmojiSlot01_Track_X,    Parent = Refs.EmojiSlot01_Track_Move },
        new() { Id = Refs.EmojiSlot01_Track_Y,    Parent = Refs.EmojiSlot01_Track_X },
        new() { Id = Refs.EmojiSlot01_Scale,      Parent = Refs.EmojiSlot01_Track_Y },
        new() { Id = Refs.EmojiSlot01_Rotation,   Parent = Refs.EmojiSlot01_Scale },
        new() { Id = Refs.EmojiSlot01_Image,      Parent = Refs.EmojiSlot01_Rotation, NeedsImage = true },
    
        // Emoji02 casting/effect axis
        new() { Id = Refs.CharacterEmojiSlot02_Root,          Parent = Refs.Character_CastTransform, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterEmojiSlot02_CastTransform, Parent = Refs.CharacterEmojiSlot02_Root },
        new() { Id = Refs.CharacterEmojiSlot02_Effect,        Parent = Refs.CharacterEmojiSlot02_CastTransform },
    
        // Emoji02 sprite motion axis
        new() { Id = Refs.EmojiSlot02_Track,      Parent = Refs.CharacterEmojiSlot02_Effect },
        new() { Id = Refs.EmojiSlot02_Track_Move, Parent = Refs.EmojiSlot02_Track },
        new() { Id = Refs.EmojiSlot02_Track_X,    Parent = Refs.EmojiSlot02_Track_Move },
        new() { Id = Refs.EmojiSlot02_Track_Y,    Parent = Refs.EmojiSlot02_Track_X },
        new() { Id = Refs.EmojiSlot02_Scale,      Parent = Refs.EmojiSlot02_Track_Y },
        new() { Id = Refs.EmojiSlot02_Rotation,   Parent = Refs.EmojiSlot02_Scale },
        new() { Id = Refs.EmojiSlot02_Image,      Parent = Refs.EmojiSlot02_Rotation, NeedsImage = true },
    };
}

public enum CharacterRigTarget
{
    // Slot axis - stage placement
    CharSlot_Anchor,
    CharSlot_Track,
    CharSlot_Track_X,
    CharSlot_Track_Y,
    CharSlot_Rotation,
    CharSlot_SwayPivot,
    CharSlot_Scale,

    // Framing axis - pseudo camera / focus response
    CharSlot_FramingTransform,
    CharSlot_FramingScale,
    CharSlot_FramingScale_X,
    CharSlot_FramingScale_Y,

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

    // Emoji00 casting/effect axis
    CharacterEmojiSlot00_Root,
    CharacterEmojiSlot00_CastTransform,
    CharacterEmojiSlot00_Effect,

    // Emoji00 sprite motion axis
    EmojiSlot00_Track,
    EmojiSlot00_Track_Move,
    EmojiSlot00_Track_X,
    EmojiSlot00_Track_Y,
    EmojiSlot00_Scale,
    EmojiSlot00_Rotation,
    EmojiSlot00_Image,

    // Emoji01
    CharacterEmojiSlot01_Root,
    CharacterEmojiSlot01_CastTransform,
    CharacterEmojiSlot01_Effect,

    EmojiSlot01_Track,
    EmojiSlot01_Track_Move,
    EmojiSlot01_Track_X,
    EmojiSlot01_Track_Y,
    EmojiSlot01_Scale,
    EmojiSlot01_Rotation,
    EmojiSlot01_Image,

    // Emoji02
    CharacterEmojiSlot02_Root,
    CharacterEmojiSlot02_CastTransform,
    CharacterEmojiSlot02_Effect,

    EmojiSlot02_Track,
    EmojiSlot02_Track_Move,
    EmojiSlot02_Track_X,
    EmojiSlot02_Track_Y,
    EmojiSlot02_Scale,
    EmojiSlot02_Rotation,
    EmojiSlot02_Image
}

public sealed class CharacterRigRefs
{
    public RectTransform RigRoot { get; private set; }
    public CharacterRigRefs(RectTransform rigRoot) => RigRoot = rigRoot;
    
    public CharacterEmojiMaterialRuntime EmojiSlot00_MaterialRuntime;
    public CharacterEmojiMaterialRuntime EmojiSlot01_MaterialRuntime;
    public CharacterEmojiMaterialRuntime EmojiSlot02_MaterialRuntime;
    
    // Visual effect: CharacterPortraitSprite_Image에 바인딩된 runtime material 소유자.
    // SetupCharRigCommand가 생성, CharacterRigRegistry.DestroyRig가 Dispose.
    public CharacterRigVisualEffectController VisualEffect;

    // Slot axis - stage placement
    public RectTransform CharSlot_Anchor;
    public RectTransform CharSlot_Track;
    public RectTransform CharSlot_Track_X;
    public RectTransform CharSlot_Track_Y;
    public RectTransform CharSlot_Rotation;
    public RectTransform CharSlot_SwayPivot;
    public RectTransform CharSlot_Scale;

    // Framing axis - pseudo camera / focus response
    public RectTransform CharSlot_FramingTransform;
    public RectTransform CharSlot_FramingScale;
    public RectTransform CharSlot_FramingScale_X;
    public RectTransform CharSlot_FramingScale_Y;

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
    public RectTransform CharacterEmojiSlot00_Effect;

    // Emoji00 sprite motion axis
    public RectTransform EmojiSlot00_Track;
    public RectTransform EmojiSlot00_Track_Move;
    public RectTransform EmojiSlot00_Track_X;
    public RectTransform EmojiSlot00_Track_Y;
    public RectTransform EmojiSlot00_Scale;
    public RectTransform EmojiSlot00_Rotation;
    public Image         EmojiSlot00_Image;

    // Emoji01 casting/effect axis
    public RectTransform CharacterEmojiSlot01_Root;
    public RectTransform CharacterEmojiSlot01_CastTransform;
    public RectTransform CharacterEmojiSlot01_Effect;

    // Emoji01 sprite motion axis
    public RectTransform EmojiSlot01_Track;
    public RectTransform EmojiSlot01_Track_Move;
    public RectTransform EmojiSlot01_Track_X;
    public RectTransform EmojiSlot01_Track_Y;
    public RectTransform EmojiSlot01_Scale;
    public RectTransform EmojiSlot01_Rotation;
    public Image         EmojiSlot01_Image;

    // Emoji02 casting/effect axis
    public RectTransform CharacterEmojiSlot02_Root;
    public RectTransform CharacterEmojiSlot02_CastTransform;
    public RectTransform CharacterEmojiSlot02_Effect;

    // Emoji02 sprite motion axis
    public RectTransform EmojiSlot02_Track;
    public RectTransform EmojiSlot02_Track_Move;
    public RectTransform EmojiSlot02_Track_X;
    public RectTransform EmojiSlot02_Track_Y;
    public RectTransform EmojiSlot02_Scale;
    public RectTransform EmojiSlot02_Rotation;
    public Image         EmojiSlot02_Image;
}

public static class CharacterRigRefsExtensions
{
    public static CharacterEmojiMaterialRuntime GetEmojiMaterialRuntime(
        this CharacterRigRefs refs,
        CharacterRigTarget imageTarget)
    {
        return imageTarget switch
        {
            CharacterRigTarget.EmojiSlot00_Image => refs.EmojiSlot00_MaterialRuntime,
            CharacterRigTarget.EmojiSlot01_Image => refs.EmojiSlot01_MaterialRuntime,
            CharacterRigTarget.EmojiSlot02_Image => refs.EmojiSlot02_MaterialRuntime,
            _ => null
        };
    }
    
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
            CharacterRigTarget.CharSlot_Anchor    => refs.CharSlot_Anchor,
            CharacterRigTarget.CharSlot_Track     => refs.CharSlot_Track,
            CharacterRigTarget.CharSlot_Track_X   => refs.CharSlot_Track_X,
            CharacterRigTarget.CharSlot_Track_Y   => refs.CharSlot_Track_Y,
            CharacterRigTarget.CharSlot_Rotation  => refs.CharSlot_Rotation,
            CharacterRigTarget.CharSlot_SwayPivot => refs.CharSlot_SwayPivot,
            CharacterRigTarget.CharSlot_Scale     => refs.CharSlot_Scale,

            // Framing axis - pseudo camera / focus response
            CharacterRigTarget.CharSlot_FramingTransform => refs.CharSlot_FramingTransform,
            CharacterRigTarget.CharSlot_FramingScale     => refs.CharSlot_FramingScale,
            CharacterRigTarget.CharSlot_FramingScale_X   => refs.CharSlot_FramingScale_X,
            CharacterRigTarget.CharSlot_FramingScale_Y   => refs.CharSlot_FramingScale_Y,

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
            CharacterRigTarget.CharacterPortrait_ActingScale    => refs.CharacterPortrait_ActingScale,
            CharacterRigTarget.CharacterPortrait_ActingScale_X  => refs.CharacterPortrait_ActingScale_X,
            CharacterRigTarget.CharacterPortrait_ActingScale_Y  => refs.CharacterPortrait_ActingScale_Y,

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
            CharacterRigTarget.CharacterEmojiSlot00_Effect        => refs.CharacterEmojiSlot00_Effect,

            // Emoji00 sprite motion axis
            CharacterRigTarget.EmojiSlot00_Track      => refs.EmojiSlot00_Track,
            CharacterRigTarget.EmojiSlot00_Track_Move => refs.EmojiSlot00_Track_Move,
            CharacterRigTarget.EmojiSlot00_Track_X    => refs.EmojiSlot00_Track_X,
            CharacterRigTarget.EmojiSlot00_Track_Y    => refs.EmojiSlot00_Track_Y,
            CharacterRigTarget.EmojiSlot00_Scale      => refs.EmojiSlot00_Scale,
            CharacterRigTarget.EmojiSlot00_Rotation   => refs.EmojiSlot00_Rotation,
            CharacterRigTarget.EmojiSlot00_Image      => refs.EmojiSlot00_Image,

            // Emoji01 casting/effect axis
            CharacterRigTarget.CharacterEmojiSlot01_Root          => refs.CharacterEmojiSlot01_Root,
            CharacterRigTarget.CharacterEmojiSlot01_CastTransform => refs.CharacterEmojiSlot01_CastTransform,
            CharacterRigTarget.CharacterEmojiSlot01_Effect        => refs.CharacterEmojiSlot01_Effect,

            // Emoji01 sprite motion axis
            CharacterRigTarget.EmojiSlot01_Track      => refs.EmojiSlot01_Track,
            CharacterRigTarget.EmojiSlot01_Track_Move => refs.EmojiSlot01_Track_Move,
            CharacterRigTarget.EmojiSlot01_Track_X    => refs.EmojiSlot01_Track_X,
            CharacterRigTarget.EmojiSlot01_Track_Y    => refs.EmojiSlot01_Track_Y,
            CharacterRigTarget.EmojiSlot01_Scale      => refs.EmojiSlot01_Scale,
            CharacterRigTarget.EmojiSlot01_Rotation   => refs.EmojiSlot01_Rotation,
            CharacterRigTarget.EmojiSlot01_Image      => refs.EmojiSlot01_Image,

            // Emoji02 casting/effect axis
            CharacterRigTarget.CharacterEmojiSlot02_Root          => refs.CharacterEmojiSlot02_Root,
            CharacterRigTarget.CharacterEmojiSlot02_CastTransform => refs.CharacterEmojiSlot02_CastTransform,
            CharacterRigTarget.CharacterEmojiSlot02_Effect        => refs.CharacterEmojiSlot02_Effect,

            // Emoji02 sprite motion axis
            CharacterRigTarget.EmojiSlot02_Track      => refs.EmojiSlot02_Track,
            CharacterRigTarget.EmojiSlot02_Track_Move => refs.EmojiSlot02_Track_Move,
            CharacterRigTarget.EmojiSlot02_Track_X    => refs.EmojiSlot02_Track_X,
            CharacterRigTarget.EmojiSlot02_Track_Y    => refs.EmojiSlot02_Track_Y,
            CharacterRigTarget.EmojiSlot02_Scale      => refs.EmojiSlot02_Scale,
            CharacterRigTarget.EmojiSlot02_Rotation   => refs.EmojiSlot02_Rotation,
            CharacterRigTarget.EmojiSlot02_Image      => refs.EmojiSlot02_Image,

            _ => null
        };
    }
}