using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CharacterRigTarget
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
    CharacterEmoji_Image,
}

public sealed class CharacterRigRefs
{
    // Root axis
    public RectTransform Character_Anchor;
    public RectTransform Character_Track;
    
    // Translate split layers
    public RectTransform Character_Track_Move;
    public RectTransform Character_Track_X;
    public RectTransform Character_Track_Y;

    // Portrait
    public RectTransform CharacterPortrait_Root;
    public RectTransform CharacterPortrait_Pad;
    public RectTransform CharacterPortrait_SwayPivot;
    public RectTransform CharacterPortrait_Shake;
    public RectTransform CharacterPortrait_Scale;
    public Image         CharacterPortrait_Image;

    // PortraitOverlay
    public RectTransform CharacterPortraitOverlay_Root;
    public Image         CharacterPortraitOverlay_Image;

    // Emoji
    public RectTransform CharacterEmoji_Root;
    public RectTransform CharacterEmoji_Anchor;
    public RectTransform CharacterEmoji_Pad;
    public RectTransform CharacterEmoji_Track;
    public RectTransform CharacterEmoji_Scale;
    public RectTransform CharacterEmoji_SwayPivot;
    public Image         CharacterEmoji_Image;
}

public static class RigRegistryExt
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
            // Root axis
            CharacterRigTarget.Character_Anchor     => refs.Character_Anchor,
            CharacterRigTarget.Character_Track      => refs.Character_Track,
            CharacterRigTarget.Character_Track_Move => refs.Character_Track_Move,
            CharacterRigTarget.Character_Track_X    => refs.Character_Track_X,
            CharacterRigTarget.Character_Track_Y    => refs.Character_Track_Y,
            
            // Portrait
            CharacterRigTarget.CharacterPortrait_Root      => refs.CharacterPortrait_Root,
            CharacterRigTarget.CharacterPortrait_Pad       => refs.CharacterPortrait_Pad,
            CharacterRigTarget.CharacterPortrait_SwayPivot => refs.CharacterPortrait_SwayPivot,
            CharacterRigTarget.CharacterPortrait_Shake     => refs.CharacterPortrait_Shake,
            CharacterRigTarget.CharacterPortrait_Scale     => refs.CharacterPortrait_Scale,
            CharacterRigTarget.CharacterPortrait_Image     => refs.CharacterPortrait_Image,
            
            // PortraitOverlay
            CharacterRigTarget.CharacterPortraitOverlay_Root  => refs.CharacterPortraitOverlay_Root,
            CharacterRigTarget.CharacterPortraitOverlay_Image => refs.CharacterPortraitOverlay_Image,

            // Emoji
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
    
    public static Graphic GetGraphic(this CharacterRigRefs refs, CharacterRigTarget target)
        => refs.GetComponent(target) as Graphic;

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