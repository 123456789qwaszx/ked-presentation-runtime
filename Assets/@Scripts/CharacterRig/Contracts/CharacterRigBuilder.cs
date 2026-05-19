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
            Debug.LogWarning($"[CharacterRigBuilder] Missing bound ref '{key}'.");
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
            Debug.LogWarning($"[CharacterRigBuilder] Missing Image on '{rt.name}'.");
            return null;
        }

        return img;
    }

    // Slot axis - stage placement
    refs.CharSlot_Anchor = GetRt(CharacterRigSchema.Refs.CharSlot_Anchor);
    refs.CharSlot_Track = GetRt(CharacterRigSchema.Refs.CharSlot_Track);
    refs.CharSlot_Track_Move = GetRt(CharacterRigSchema.Refs.CharSlot_Track_Move);
    refs.CharSlot_Track_X = GetRt(CharacterRigSchema.Refs.CharSlot_Track_X);
    refs.CharSlot_Track_Y = GetRt(CharacterRigSchema.Refs.CharSlot_Track_Y);
    refs.CharSlot_Rotation = GetRt(CharacterRigSchema.Refs.CharSlot_Rotation);
    refs.CharSlot_Scale = GetRt(CharacterRigSchema.Refs.CharSlot_Scale);

    // Framing axis - pseudo camera / focus response
    refs.CharSlot_FramingTransform = GetRt(CharacterRigSchema.Refs.CharSlot_FramingTransform);
    refs.CharSlot_FramingScale = GetRt(CharacterRigSchema.Refs.CharSlot_FramingScale);
    refs.CharSlot_FramingScale_X = GetRt(CharacterRigSchema.Refs.CharSlot_FramingScale_X);
    refs.CharSlot_FramingScale_Y = GetRt(CharacterRigSchema.Refs.CharSlot_FramingScale_Y);

    // Character casting axis - per-character defaults
    refs.Character_Root = GetRt(CharacterRigSchema.Refs.Character_Root);
    refs.Character_CastLocation = GetRt(CharacterRigSchema.Refs.Character_CastLocation);
    refs.Character_CastSize = GetRt(CharacterRigSchema.Refs.Character_CastSize);
    refs.Character_CastAngle = GetRt(CharacterRigSchema.Refs.Character_CastAngle);

    // Portrait acting axis
    refs.CharacterPortrait_Track = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track);
    refs.CharacterPortrait_Track_Move = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track_Move);
    refs.CharacterPortrait_Track_X = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track_X);
    refs.CharacterPortrait_Track_Y = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track_Y);
    refs.CharacterPortrait_Rotation = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Rotation);
    refs.CharacterPortrait_SwayPivot = GetRt(CharacterRigSchema.Refs.CharacterPortrait_SwayPivot);
    refs.CharacterPortrait_Shake = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Shake);
    refs.CharacterPortrait_ActingScale = GetRt(CharacterRigSchema.Refs.CharacterPortrait_ActingScale);
    refs.CharacterPortrait_ActingScale_X = GetRt(CharacterRigSchema.Refs.CharacterPortrait_ActingScale_X);
    refs.CharacterPortrait_ActingScale_Y = GetRt(CharacterRigSchema.Refs.CharacterPortrait_ActingScale_Y);

    // Portrait sprite
    refs.CharacterPortraitSprite_Root = GetRt(CharacterRigSchema.Refs.CharacterPortraitSprite_Root);
    refs.CharacterPortraitSprite_Image = GetImg(CharacterRigSchema.Refs.CharacterPortraitSprite_Image);

    // Portrait sprite overlay
    refs.CharacterPortraitSpriteOverlay_Root = GetRt(CharacterRigSchema.Refs.CharacterPortraitSpriteOverlay_Root);
    refs.CharacterPortraitSpriteOverlay_Image = GetImg(CharacterRigSchema.Refs.CharacterPortraitSpriteOverlay_Image);

    // Portrait extension / preserved systems
    refs.Character_ExtensionsRoot = GetRt(CharacterRigSchema.Refs.Character_ExtensionsRoot);

    // Emoji00 casting/effect axis
    refs.CharacterEmojiSlot00_Root = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_Root);
    refs.CharacterEmojiSlot00_Location = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_Location);
    refs.CharacterEmojiSlot00_Size = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_Size);
    refs.CharacterEmojiSlot00_Angle = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_Angle);
    refs.CharacterEmojiSlot00_Effect = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_Effect);

    // Emoji00 sprite motion axis
    refs.EmojiSlot00_Track = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Track);
    refs.EmojiSlot00_Track_Move = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Track_Move);
    refs.EmojiSlot00_Track_X = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Track_X);
    refs.EmojiSlot00_Track_Y = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Track_Y);
    refs.EmojiSlot00_Scale = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Scale);
    refs.EmojiSlot00_Rotation = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Rotation);
    refs.EmojiSlot00_Image = GetImg(CharacterRigSchema.Refs.EmojiSlot00_Image);

    // Emoji01 casting/effect axis
    refs.CharacterEmojiSlot01_Root = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_Root);
    refs.CharacterEmojiSlot01_Location = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_Location);
    refs.CharacterEmojiSlot01_Size = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_Size);
    refs.CharacterEmojiSlot01_Angle = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_Angle);
    refs.CharacterEmojiSlot01_Effect = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_Effect);

    // Emoji01 sprite motion axis
    refs.EmojiSlot01_Track = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Track);
    refs.EmojiSlot01_Track_Move = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Track_Move);
    refs.EmojiSlot01_Track_X = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Track_X);
    refs.EmojiSlot01_Track_Y = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Track_Y);
    refs.EmojiSlot01_Scale = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Scale);
    refs.EmojiSlot01_Rotation = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Rotation);
    refs.EmojiSlot01_Image = GetImg(CharacterRigSchema.Refs.EmojiSlot01_Image);

    // Emoji02 casting/effect axis
    refs.CharacterEmojiSlot02_Root = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_Root);
    refs.CharacterEmojiSlot02_Location = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_Location);
    refs.CharacterEmojiSlot02_Size = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_Size);
    refs.CharacterEmojiSlot02_Angle = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_Angle);
    refs.CharacterEmojiSlot02_Effect = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_Effect);

    // Emoji02 sprite motion axis
    refs.EmojiSlot02_Track = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Track);
    refs.EmojiSlot02_Track_Move = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Track_Move);
    refs.EmojiSlot02_Track_X = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Track_X);
    refs.EmojiSlot02_Track_Y = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Track_Y);
    refs.EmojiSlot02_Scale = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Scale);
    refs.EmojiSlot02_Rotation = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Rotation);
    refs.EmojiSlot02_Image = GetImg(CharacterRigSchema.Refs.EmojiSlot02_Image);

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