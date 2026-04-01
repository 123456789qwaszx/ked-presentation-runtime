using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CharacterRigTarget
{
    // Root axis
    Character_Anchor,
    Character_Track,
    
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
    
    private static string ToCodePoints(string s)
    {
        if (s == null) return "null";

        var chars = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            chars.Append(((int)s[i]).ToString());
            if (i < s.Length - 1)
                chars.Append(", ");
        }
        return chars.ToString();
    }
    
    public static bool TryGetCharRigRefs(this Dictionary<string, object> rigRegistry, string roleKey, out CharacterRigRefs rigRefs)
    {
        Debug.Log($"{roleKey}");
        Debug.Log($"[TryGetCharRigRefs] registry count = {rigRegistry?.Count ?? -1}");
        foreach (var key in rigRegistry.Keys)
        {
            Debug.Log($"stored key = '{key}' / codes = {ToCodePoints(key)}");
        }
        
        foreach (var value in rigRegistry.Values)
        {
            Debug.Log($"value={value}, type={value?.GetType().FullName ?? "null"}");
        }

        if (rigRegistry.TryGetValue(roleKey, out var obj))
        {
            Debug.Log($"[TryGetCharRigRefs] key={roleKey}, valueType={obj?.GetType().FullName ?? "null"}");

            if (obj is CharacterRigRefs refs)
            {
                rigRefs = refs;
                return true;
            }

            Debug.LogWarning($"[TryGetCharRigRefs] key '{roleKey}' exists, but value is not CharacterRigRefs.");
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
            CharacterRigTarget.Character_Anchor => refs.Character_Anchor,
            CharacterRigTarget.Character_Track  => refs.Character_Track,

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