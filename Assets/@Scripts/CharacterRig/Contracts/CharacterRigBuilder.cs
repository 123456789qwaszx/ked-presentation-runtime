using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CharacterRigBuilder
{
    public RectTransform BuildCharacterRigRoot(RectTransform rigPrefab = null, string rolePrefix = "", string rigRootName = "CharacterRig")
    {
        RectTransform rigRoot;

        if (rigPrefab != null)
        {
            rigRoot = Object.Instantiate(rigPrefab);
            rigRoot.name = WithRole(rolePrefix, rigRootName);

            if (!string.IsNullOrEmpty(rolePrefix))
                PrefixAllChildren(rigRoot.transform, rolePrefix);
        }
        else
        {
            GameObject rootGo = new(WithRole(rolePrefix, rigRootName), typeof(RectTransform));
            rigRoot = (RectTransform)rootGo.transform;
        
            StretchFull(rigRoot);
            EnsureGraph(rigRoot, rolePrefix);
        }
        
        return rigRoot;
    }
    
    public void BindRefsFromRoot(RectTransform rigRoot, string rolePrefix, out CharacterRigRefs refs)
    {
        Dictionary<CharacterRigSchema.Refs, RectTransform> map = CreateRefMap(rigRoot, rolePrefix);
        EnsureValidGraphMap(rigRoot, rolePrefix, ref map);
        
        refs = BindRefs(rigRoot, map);
    }
    
    
    private void EnsureValidGraphMap(RectTransform rigRoot, string rolePrefix, ref Dictionary<CharacterRigSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(CharacterRigSchema.Refs)).Length;
        
        if (map.Count >= expectedCount)
            return;
        
        Debug.LogWarning($"[CharacterRigBuilder] Invalid rig graph. Rebuilding from 'CharacterRigSchema'. rigRoot='{rigRoot.name}'.");
        for (int i = rigRoot.childCount - 1; i >= 0; i--)
            Object.Destroy(rigRoot.GetChild(i).gameObject);
        
        EnsureGraph(rigRoot, rolePrefix);
        
        map = CreateRefMap(rigRoot, rolePrefix);
    }
    
    #region Auto Create Graph
    private void EnsureGraph(RectTransform root, string rolePrefix)
    {
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.Character_Anchor, null);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.Character_Track, CharacterRigSchema.Refs.Character_Anchor);

        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.Character_Track_Move, CharacterRigSchema.Refs.Character_Track);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.Character_Track_X, CharacterRigSchema.Refs.Character_Track_Move);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.Character_Track_Y, CharacterRigSchema.Refs.Character_Track_X);

        // Portrait
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortrait_Root, CharacterRigSchema.Refs.Character_Track_Y);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortrait_Pad, CharacterRigSchema.Refs.CharacterPortrait_Root);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortrait_SwayPivot, CharacterRigSchema.Refs.CharacterPortrait_Pad);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortrait_Shake, CharacterRigSchema.Refs.CharacterPortrait_SwayPivot);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortrait_Scale, CharacterRigSchema.Refs.CharacterPortrait_Shake);
        EnsureImage(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortrait_Image, CharacterRigSchema.Refs.CharacterPortrait_Scale);

        // Portrait overlays
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortraitOverlay_Root, CharacterRigSchema.Refs.CharacterPortrait_Scale);
        EnsureImage(root, rolePrefix, CharacterRigSchema.Refs.CharacterPortraitOverlay_Image, CharacterRigSchema.Refs.CharacterPortraitOverlay_Root);

        // Emoji
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterEmoji_Root, CharacterRigSchema.Refs.Character_Track_Y);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterEmoji_Anchor, CharacterRigSchema.Refs.CharacterEmoji_Root);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterEmoji_Pad, CharacterRigSchema.Refs.CharacterEmoji_Anchor);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterEmoji_Track, CharacterRigSchema.Refs.CharacterEmoji_Pad);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterEmoji_Scale, CharacterRigSchema.Refs.CharacterEmoji_Track);
        EnsureNode(root, rolePrefix, CharacterRigSchema.Refs.CharacterEmoji_SwayPivot, CharacterRigSchema.Refs.CharacterEmoji_Scale);
        EnsureImage(root, rolePrefix, CharacterRigSchema.Refs.CharacterEmoji_Image, CharacterRigSchema.Refs.CharacterEmoji_SwayPivot);
    }

    private void EnsureNode(RectTransform rigRoot, string rolePrefix, CharacterRigSchema.Refs id, CharacterRigSchema.Refs? parent)
    {
        RectTransform parentRt = parent.HasValue
            ? FindByName(rigRoot, WithRole(rolePrefix, parent.Value.ToString())) as RectTransform
            : rigRoot;

        RectTransform rt = EnsureRect(parentRt, WithRole(rolePrefix, id.ToString()));

        if (NeedsBottomPivot(id))
            rt.pivot = new Vector2(0.5f, 0f);

        if (NeedsCanvasGroup(id) && !rt.TryGetComponent<CanvasGroup>(out _))
            rt.gameObject.AddComponent<CanvasGroup>();
    }

    private bool NeedsBottomPivot(CharacterRigSchema.Refs id)
    {
        return id == CharacterRigSchema.Refs.CharacterPortrait_SwayPivot ||
               id == CharacterRigSchema.Refs.CharacterEmoji_SwayPivot;
    }

    private bool NeedsCanvasGroup(CharacterRigSchema.Refs id)
    {
        return id == CharacterRigSchema.Refs.CharacterPortrait_Root ||
               id == CharacterRigSchema.Refs.CharacterPortraitOverlay_Root ||
               id == CharacterRigSchema.Refs.CharacterEmoji_Root;
        //return id.ToString().EndsWith("_Root", StringComparison.Ordinal);
    }
    
    private RectTransform EnsureRect(RectTransform parent, string name)
    {
        RectTransform existing = FindByName(parent, name) as RectTransform;
        if (existing != null)
            return existing;

        GameObject go = new(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        StretchFull(rt);

        return rt;
    }

    private void EnsureImage(RectTransform rigRoot, string rolePrefix, CharacterRigSchema.Refs id, CharacterRigSchema.Refs parent)
    {
        var parentRt = FindByName(rigRoot, WithRole(rolePrefix, parent.ToString())) as RectTransform;
        var rt = EnsureRect(parentRt, WithRole(rolePrefix, id.ToString()));

        if (!rt.TryGetComponent<Image>(out _))
            rt.gameObject.AddComponent<Image>();
    }
    #endregion
    
    #region Binding / Refs
    private Dictionary<CharacterRigSchema.Refs, RectTransform> CreateRefMap(RectTransform rigRoot, string rolePrefix)
    {
        Dictionary<CharacterRigSchema.Refs, RectTransform> map = new();

        foreach (CharacterRigSchema.Refs id in Enum.GetValues(typeof(CharacterRigSchema.Refs)))
        {
            string nodeName = WithRole(rolePrefix, id.ToString());
            RectTransform t = FindByName(rigRoot, nodeName) as RectTransform;

            if (t != null)
                map[id] = t;
        }

        return map;
    }
    
    private CharacterRigRefs BindRefs(RectTransform rigRoot, Dictionary<CharacterRigSchema.Refs, RectTransform> map)
    {
        CharacterRigRefs refs = new();

        refs.RigRoot = rigRoot;

        RectTransform GetRt(CharacterRigSchema.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform targetRect) || targetRect == null)
            {
                Debug.LogWarning($"[SetRig] Missing bound ref '{key}'.");

                return null;
            }

            return targetRect;
        }

        Image GetImg(CharacterRigSchema.Refs key)
        {
            RectTransform rt = GetRt(key);
            if (rt == null)
                return null;

            Image img = rt.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning($"[SetRig] Missing Image on '{rt.name}'.");
                return null;
            }

            return img;
        }

        // Root axis
        refs.Character_Anchor = GetRt(CharacterRigSchema.Refs.Character_Anchor);
        refs.Character_Track = GetRt(CharacterRigSchema.Refs.Character_Track);

        refs.Character_Track_Move = GetRt(CharacterRigSchema.Refs.Character_Track_Move);
        refs.Character_Track_X = GetRt(CharacterRigSchema.Refs.Character_Track_X);
        refs.Character_Track_Y = GetRt(CharacterRigSchema.Refs.Character_Track_Y);

        // Portrait
        refs.CharacterPortrait_Root = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Root);
        refs.CharacterPortrait_Pad = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Pad);
        refs.CharacterPortrait_SwayPivot = GetRt(CharacterRigSchema.Refs.CharacterPortrait_SwayPivot);
        refs.CharacterPortrait_Shake = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Shake);
        refs.CharacterPortrait_Scale = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Scale);
        refs.CharacterPortrait_Image = GetImg(CharacterRigSchema.Refs.CharacterPortrait_Image);

        // Portrait overlays
        refs.CharacterPortraitOverlay_Root = GetRt(CharacterRigSchema.Refs.CharacterPortraitOverlay_Root);
        refs.CharacterPortraitOverlay_Image = GetImg(CharacterRigSchema.Refs.CharacterPortraitOverlay_Image);

        // Emoji
        refs.CharacterEmoji_Root = GetRt(CharacterRigSchema.Refs.CharacterEmoji_Root);
        refs.CharacterEmoji_Anchor = GetRt(CharacterRigSchema.Refs.CharacterEmoji_Anchor);
        refs.CharacterEmoji_Pad = GetRt(CharacterRigSchema.Refs.CharacterEmoji_Pad);
        refs.CharacterEmoji_Track = GetRt(CharacterRigSchema.Refs.CharacterEmoji_Track);
        refs.CharacterEmoji_Scale = GetRt(CharacterRigSchema.Refs.CharacterEmoji_Scale);
        refs.CharacterEmoji_SwayPivot = GetRt(CharacterRigSchema.Refs.CharacterEmoji_SwayPivot);
        refs.CharacterEmoji_Image = GetImg(CharacterRigSchema.Refs.CharacterEmoji_Image);

        return refs;
    }
    #endregion
    
    #region Helper
    private Transform FindByName(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindByName(child, name);

            if (found != null)
                return found;
        }

        return null;
    }
    
    private void PrefixAllChildren(Transform root, string rolePrefix)
    {
        if (root == null || string.IsNullOrEmpty(rolePrefix))
            return;

        void Walk(Transform t)
        {
            t.name = WithRole(rolePrefix, t.name);

            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i));
        }

        Walk(root);
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    
    private string WithRole(string rolePrefix, string baseName)
    {
        if (string.IsNullOrEmpty(rolePrefix))
            return baseName;

        if (baseName.StartsWith(rolePrefix, StringComparison.Ordinal))
            return baseName;

        return $"{rolePrefix}{baseName}";
    }
    #endregion
}