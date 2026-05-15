using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CharacterRigAccess
{
    public RectTransform CreateCharacterRig(RectTransform rigPrefab = null, string rolePrefix = "", string rigRootName = "CharacterRig")
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
    
    public CharacterRigRefs BuildRefsFromRoot(RectTransform rigRoot, string rolePrefix)
    {
        var map = BindMap(rigRoot, rolePrefix);
        return BuildRefs(rigRoot, map);
    }
    
    
    #region Auto Create Graph
    private void EnsureGraph(RectTransform root, string rolePrefix)
    {
        EnsureNode(root, rolePrefix, CharacterRig.Refs.Character_Anchor, null);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.Character_Track, CharacterRig.Refs.Character_Anchor);

        EnsureNode(root, rolePrefix, CharacterRig.Refs.Character_Track_Move, CharacterRig.Refs.Character_Track);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.Character_Track_X, CharacterRig.Refs.Character_Track_Move);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.Character_Track_Y, CharacterRig.Refs.Character_Track_X);

        // Portrait
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterPortrait_Root, CharacterRig.Refs.Character_Track_Y);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterPortrait_Pad, CharacterRig.Refs.CharacterPortrait_Root);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterPortrait_SwayPivot, CharacterRig.Refs.CharacterPortrait_Pad);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterPortrait_Shake, CharacterRig.Refs.CharacterPortrait_SwayPivot);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterPortrait_Scale, CharacterRig.Refs.CharacterPortrait_Shake);
        EnsureImage(root, rolePrefix, CharacterRig.Refs.CharacterPortrait_Image, CharacterRig.Refs.CharacterPortrait_Scale);

        // Portrait overlays
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterPortraitOverlay_Root, CharacterRig.Refs.CharacterPortrait_Scale);
        EnsureImage(root, rolePrefix, CharacterRig.Refs.CharacterPortraitOverlay_Image, CharacterRig.Refs.CharacterPortraitOverlay_Root);

        // Emoji
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterEmoji_Root, CharacterRig.Refs.Character_Track_Y);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterEmoji_Anchor, CharacterRig.Refs.CharacterEmoji_Root);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterEmoji_Pad, CharacterRig.Refs.CharacterEmoji_Anchor);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterEmoji_Track, CharacterRig.Refs.CharacterEmoji_Pad);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterEmoji_Scale, CharacterRig.Refs.CharacterEmoji_Track);
        EnsureNode(root, rolePrefix, CharacterRig.Refs.CharacterEmoji_SwayPivot, CharacterRig.Refs.CharacterEmoji_Scale);
        EnsureImage(root, rolePrefix, CharacterRig.Refs.CharacterEmoji_Image, CharacterRig.Refs.CharacterEmoji_SwayPivot);
    }

    private void EnsureNode(RectTransform rigRoot, string rolePrefix, CharacterRig.Refs id, CharacterRig.Refs? parent)
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

    private bool NeedsBottomPivot(CharacterRig.Refs id)
    {
        return id == CharacterRig.Refs.CharacterPortrait_SwayPivot ||
               id == CharacterRig.Refs.CharacterEmoji_SwayPivot;
    }

    private bool NeedsCanvasGroup(CharacterRig.Refs id)
    {
        return id == CharacterRig.Refs.CharacterPortrait_Root ||
               id == CharacterRig.Refs.CharacterPortraitOverlay_Root ||
               id == CharacterRig.Refs.CharacterEmoji_Root;
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

    private void EnsureImage(RectTransform rigRoot, string rolePrefix, CharacterRig.Refs id, CharacterRig.Refs parent)
    {
        var parentRt = FindByName(rigRoot, WithRole(rolePrefix, parent.ToString())) as RectTransform;
        var rt = EnsureRect(parentRt, WithRole(rolePrefix, id.ToString()));

        if (!rt.TryGetComponent<Image>(out _))
            rt.gameObject.AddComponent<Image>();
    }
    #endregion
    
    #region Binding / Refs
    private Dictionary<CharacterRig.Refs, RectTransform> BindMap(RectTransform rigRoot, string rolePrefix)
    {
        Dictionary<CharacterRig.Refs, RectTransform> map = new();

        foreach (CharacterRig.Refs id in Enum.GetValues(typeof(CharacterRig.Refs)))
        {
            string nodeName = WithRole(rolePrefix, id.ToString());

            RectTransform t = FindByName(rigRoot, nodeName) as RectTransform;
            if (t == null)
            {
                Debug.LogWarning($"[SetRig] Missing node '{nodeName}' under '{rigRoot.name}'.");
                continue;
            }

            map[id] = t;
        }

        return map;
    }
    
    private CharacterRigRefs BuildRefs(RectTransform rigRoot, Dictionary<CharacterRig.Refs, RectTransform> map)
    {
        CharacterRigRefs refs = new();

        refs.CharacterRig = rigRoot;

        RectTransform GetRt(CharacterRig.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform targetRect) || targetRect == null)
            {
                Debug.LogWarning($"[SetRig] Missing bound ref '{key}'.");

                return null;
            }

            return targetRect;
        }

        Image GetImg(CharacterRig.Refs key)
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
        refs.Character_Anchor = GetRt(CharacterRig.Refs.Character_Anchor);
        refs.Character_Track = GetRt(CharacterRig.Refs.Character_Track);

        refs.Character_Track_Move = GetRt(CharacterRig.Refs.Character_Track_Move);
        refs.Character_Track_X = GetRt(CharacterRig.Refs.Character_Track_X);
        refs.Character_Track_Y = GetRt(CharacterRig.Refs.Character_Track_Y);

        // Portrait
        refs.CharacterPortrait_Root = GetRt(CharacterRig.Refs.CharacterPortrait_Root);
        refs.CharacterPortrait_Pad = GetRt(CharacterRig.Refs.CharacterPortrait_Pad);
        refs.CharacterPortrait_SwayPivot = GetRt(CharacterRig.Refs.CharacterPortrait_SwayPivot);
        refs.CharacterPortrait_Shake = GetRt(CharacterRig.Refs.CharacterPortrait_Shake);
        refs.CharacterPortrait_Scale = GetRt(CharacterRig.Refs.CharacterPortrait_Scale);
        refs.CharacterPortrait_Image = GetImg(CharacterRig.Refs.CharacterPortrait_Image);

        // Portrait overlays
        refs.CharacterPortraitOverlay_Root = GetRt(CharacterRig.Refs.CharacterPortraitOverlay_Root);
        refs.CharacterPortraitOverlay_Image = GetImg(CharacterRig.Refs.CharacterPortraitOverlay_Image);

        // Emoji
        refs.CharacterEmoji_Root = GetRt(CharacterRig.Refs.CharacterEmoji_Root);
        refs.CharacterEmoji_Anchor = GetRt(CharacterRig.Refs.CharacterEmoji_Anchor);
        refs.CharacterEmoji_Pad = GetRt(CharacterRig.Refs.CharacterEmoji_Pad);
        refs.CharacterEmoji_Track = GetRt(CharacterRig.Refs.CharacterEmoji_Track);
        refs.CharacterEmoji_Scale = GetRt(CharacterRig.Refs.CharacterEmoji_Scale);
        refs.CharacterEmoji_SwayPivot = GetRt(CharacterRig.Refs.CharacterEmoji_SwayPivot);
        refs.CharacterEmoji_Image = GetImg(CharacterRig.Refs.CharacterEmoji_Image);

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