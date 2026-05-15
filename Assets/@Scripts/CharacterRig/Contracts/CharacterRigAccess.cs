using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterRigSchema
{
    public enum Refs
    {
        // Root axis
        Character_Anchor,
        Character_Track,
        Character_Track_Move,
        Character_Track_X,
        Character_Track_Y,
            
        // Portrait axis
        CharacterPortrait_Root,
        CharacterPortrait_Pad,
        CharacterPortrait_SwayPivot,
        CharacterPortrait_Shake,
        CharacterPortrait_Scale,
        CharacterPortrait_Image,
            
        // PortraitOverlay
        CharacterPortraitOverlay_Root,
        CharacterPortraitOverlay_Image,
        
        // Emoji axis
        CharacterEmoji_Root,
        CharacterEmoji_Anchor,
        CharacterEmoji_Pad,
        CharacterEmoji_Track,
        CharacterEmoji_Scale,
        CharacterEmoji_SwayPivot,
        CharacterEmoji_Image
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
        new() { Id = Refs.Character_Anchor,    Parent = null },
        new() { Id = Refs.Character_Track,     Parent = Refs.Character_Anchor },
        new() { Id = Refs.Character_Track_Move,Parent = Refs.Character_Track },
        new() { Id = Refs.Character_Track_X,   Parent = Refs.Character_Track_Move },
        new() { Id = Refs.Character_Track_Y,   Parent = Refs.Character_Track_X },

        new() { Id = Refs.CharacterPortrait_Root,      Parent = Refs.Character_Track_Y,             NeedsCanvasGroup = true },
        new() { Id = Refs.CharacterPortrait_Pad,       Parent = Refs.CharacterPortrait_Root },
        new() { Id = Refs.CharacterPortrait_SwayPivot, Parent = Refs.CharacterPortrait_Pad,         NeedsBottomPivot = true },
        new() { Id = Refs.CharacterPortrait_Shake,     Parent = Refs.CharacterPortrait_SwayPivot },
        new() { Id = Refs.CharacterPortrait_Scale,     Parent = Refs.CharacterPortrait_Shake },
        new() { Id = Refs.CharacterPortrait_Image,     Parent = Refs.CharacterPortrait_Scale,       NeedsImage = true },

        new() { Id = Refs.CharacterPortraitOverlay_Root,  Parent = Refs.CharacterPortrait_Scale,       NeedsCanvasGroup = true },
        new() { Id = Refs.CharacterPortraitOverlay_Image, Parent = Refs.CharacterPortraitOverlay_Root, NeedsImage = true },

        new() { Id = Refs.CharacterEmoji_Root,      Parent = Refs.Character_Track_Y,        NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f},
        new() { Id = Refs.CharacterEmoji_Anchor,    Parent = Refs.CharacterEmoji_Root },
        new() { Id = Refs.CharacterEmoji_Pad,       Parent = Refs.CharacterEmoji_Anchor },
        new() { Id = Refs.CharacterEmoji_Track,     Parent = Refs.CharacterEmoji_Pad },
        new() { Id = Refs.CharacterEmoji_Scale,     Parent = Refs.CharacterEmoji_Track },
        new() { Id = Refs.CharacterEmoji_SwayPivot, Parent = Refs.CharacterEmoji_Scale,     NeedsBottomPivot = true },
        new() { Id = Refs.CharacterEmoji_Image,     Parent = Refs.CharacterEmoji_SwayPivot, NeedsImage = true },
    };
}

public enum CharacterRigTarget
{
    Character_Anchor,
    Character_Track,
    Character_Track_Move,
    Character_Track_X,
    Character_Track_Y,
    
    CharacterPortrait_Root,
    CharacterPortrait_Pad,
    CharacterPortrait_SwayPivot,
    CharacterPortrait_Shake,
    CharacterPortrait_Scale,
    CharacterPortrait_Image,
    
    CharacterPortraitOverlay_Root,
    CharacterPortraitOverlay_Image,

    CharacterEmoji_Root,
    CharacterEmoji_Anchor,
    CharacterEmoji_Pad,
    CharacterEmoji_Track,
    CharacterEmoji_Scale,
    CharacterEmoji_SwayPivot,
    CharacterEmoji_Image,
}

public sealed class CharacterRigRefs
{
    public RectTransform RigRoot;
    
    public RectTransform Character_Anchor;
    public RectTransform Character_Track;
    public RectTransform Character_Track_Move;
    public RectTransform Character_Track_X;
    public RectTransform Character_Track_Y;

    public RectTransform CharacterPortrait_Root;
    public RectTransform CharacterPortrait_Pad;
    public RectTransform CharacterPortrait_SwayPivot;
    public RectTransform CharacterPortrait_Shake;
    public RectTransform CharacterPortrait_Scale;
    public Image         CharacterPortrait_Image;

    public RectTransform CharacterPortraitOverlay_Root;
    public Image         CharacterPortraitOverlay_Image;

    public RectTransform CharacterEmoji_Root;
    public RectTransform CharacterEmoji_Anchor;
    public RectTransform CharacterEmoji_Pad;
    public RectTransform CharacterEmoji_Track;
    public RectTransform CharacterEmoji_Scale;
    public RectTransform CharacterEmoji_SwayPivot;
    public Image         CharacterEmoji_Image;
}

public static class RigRegistryExtensions
{
    public static bool TryGetCharRigRefs(this Dictionary<string, object> rigRegistry, string roleKey, out CharacterRigRefs rigRefs)
    {
        if (rigRegistry.TryGetValue(roleKey, out var obj))
        {
            if (obj is CharacterRigRefs refs)
            {
                rigRefs = refs;
                return true;
            }
        }
        
        rigRefs = null;
        return false;
    }
}

public static class CharacterRigRefsExtensions
{
    public static Component GetComponent(this CharacterRigRefs refs, CharacterRigTarget target)
    {
        if (refs == null) return null;

        return target switch
        {
            CharacterRigTarget.Character_Anchor     => refs.Character_Anchor,
            CharacterRigTarget.Character_Track      => refs.Character_Track,
            CharacterRigTarget.Character_Track_Move => refs.Character_Track_Move,
            CharacterRigTarget.Character_Track_X    => refs.Character_Track_X,
            CharacterRigTarget.Character_Track_Y    => refs.Character_Track_Y,
            
            CharacterRigTarget.CharacterPortrait_Root      => refs.CharacterPortrait_Root,
            CharacterRigTarget.CharacterPortrait_Pad       => refs.CharacterPortrait_Pad,
            CharacterRigTarget.CharacterPortrait_SwayPivot => refs.CharacterPortrait_SwayPivot,
            CharacterRigTarget.CharacterPortrait_Shake     => refs.CharacterPortrait_Shake,
            CharacterRigTarget.CharacterPortrait_Scale     => refs.CharacterPortrait_Scale,
            CharacterRigTarget.CharacterPortrait_Image     => refs.CharacterPortrait_Image,
            
            CharacterRigTarget.CharacterPortraitOverlay_Root  => refs.CharacterPortraitOverlay_Root,
            CharacterRigTarget.CharacterPortraitOverlay_Image => refs.CharacterPortraitOverlay_Image,

            CharacterRigTarget.CharacterEmoji_Root      => refs.CharacterEmoji_Root,
            CharacterRigTarget.CharacterEmoji_Anchor    => refs.CharacterEmoji_Anchor,
            CharacterRigTarget.CharacterEmoji_Pad       => refs.CharacterEmoji_Pad,
            CharacterRigTarget.CharacterEmoji_Track     => refs.CharacterEmoji_Track,
            CharacterRigTarget.CharacterEmoji_Scale     => refs.CharacterEmoji_Scale,
            CharacterRigTarget.CharacterEmoji_SwayPivot => refs.CharacterEmoji_SwayPivot,
            CharacterRigTarget.CharacterEmoji_Image     => refs.CharacterEmoji_Image,

            _ => null
        };
    }
    
    public static RectTransform GetRect(this CharacterRigRefs refs, CharacterRigTarget target)
    {
        Component c = refs.GetComponent(target);
        
        if (c == null)
            return null;

        if (c is RectTransform rect)
            return rect;
        
        if (c is Graphic graphic)
            return graphic.rectTransform;
        
        return c.transform as RectTransform;
    }
}
