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
        Dictionary<CharacterRigSchema.Refs, RectTransform> map = CollectRefMap(rigRoot, rolePrefix);
        EnsureValidGraphMap(rigRoot, rolePrefix, ref map);
        
        refs = BuildRefs(rigRoot, map);
    }
    
    
    private void EnsureValidGraphMap(RectTransform rigRoot, string rolePrefix, ref Dictionary<CharacterRigSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(CharacterRigSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;
        
        Debug.LogWarning(
            $"[CharacterRigBuilder] Invalid rig graph. " +
            $"Rebuilding from CharacterRigSchema. " +
            $"The assigned CharacterRig prefab may be broken or missing required nodes. " +
            $"Check the baked result and replace the prefab with a newly baked one if needed. " +
            $"rigRoot='{rigRoot.name}'.");

        for (int i = rigRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rigRoot.GetChild(i);

            // Destroy() is delayed until the end of the frame.
            // Detach first so EnsureGraph() cannot find soon-to-be-destroyed nodes.
            child.SetParent(null, false);
            Object.Destroy(child.gameObject);
        }

        EnsureGraph(rigRoot, rolePrefix);

        map = CollectRefMap(rigRoot, rolePrefix);
    }
    
    #region Auto Create Graph
    private void EnsureGraph(RectTransform root, string rolePrefix)
    {
        foreach (CharacterRigSchema.NodeDef node in CharacterRigSchema.Nodes)
            EnsureNode(root, rolePrefix, node);
    }

    private void EnsureNode(RectTransform root, string rolePrefix, CharacterRigSchema.NodeDef node)
    {
        RectTransform parentRt = node.Parent.HasValue
            ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
            : root;

        RectTransform rt = EnsureRect(parentRt, WithRole(rolePrefix, node.Id.ToString()));

        if (node.NeedsBottomPivot)
            rt.pivot = new Vector2(0.5f, 0f);

        if (node.NeedsCanvasGroup)
        {
            if (!rt.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup))
                canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = node.InitialCanvasGroupAlpha;
        }

        if (node.NeedsImage && !rt.TryGetComponent<Image>(out _))
            rt.gameObject.AddComponent<Image>();
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
    #endregion
    
    #region Binding / Refs
    private Dictionary<CharacterRigSchema.Refs, RectTransform> CollectRefMap(RectTransform rigRoot, string rolePrefix)
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
    
    private CharacterRigRefs BuildRefs(RectTransform rigRoot, Dictionary<CharacterRigSchema.Refs, RectTransform> map)
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