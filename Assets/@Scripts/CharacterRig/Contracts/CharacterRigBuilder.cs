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

    #region Graph Recovery
    private void EnsureValidGraphMap(RectTransform rigRoot, string rolePrefix,
        ref Dictionary<CharacterRigSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(CharacterRigSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[CharacterRigBuilder] Invalid rig graph. " +
            $"Rebuilding from CharacterRigSchema. " +
            $"Prefab may be broken, or saved with another role prefix. " +
            $"For reusable prefab baking, call StripRolePrefixForBake after refs registration before saving. " +
            $"rigRoot='{rigRoot.name}', rolePrefix='{rolePrefix}'.",
            rigRoot);

        RectTransform preservedExtensionsRoot = DetachPreservedExtensionsRoot(rigRoot, rolePrefix);

        for (int i = rigRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rigRoot.GetChild(i);

            // Destroy() is delayed until the end of the frame.
            // Detach first so EnsureGraph() cannot find soon-to-be-destroyed nodes.
            child.SetParent(null, false);
            Object.Destroy(child.gameObject);
        }

        EnsureGraph(rigRoot, rolePrefix);

        ReattachPreservedExtensionsRoot(rigRoot, rolePrefix, preservedExtensionsRoot);

        map = CollectRefMap(rigRoot, rolePrefix);
    }
    
    private RectTransform DetachPreservedExtensionsRoot(RectTransform rigRoot, string rolePrefix)
    {
        string extensionRootName = WithRole(rolePrefix, nameof(CharacterRigSchema.Refs.Character_ExtensionsRoot));

        RectTransform extensionsRoot = FindByName(rigRoot, extensionRootName) as RectTransform;

        if (extensionsRoot == null)
            return null;

        extensionsRoot.SetParent(null, false);

        return extensionsRoot;
    }

    private void ReattachPreservedExtensionsRoot(RectTransform rigRoot, string rolePrefix, RectTransform preservedExtensionsRoot)
    {
        string extensionRootName = WithRole(rolePrefix, nameof(CharacterRigSchema.Refs.Character_ExtensionsRoot));
        string extensionParentName = WithRole(rolePrefix, nameof(CharacterRigSchema.Refs.CharacterPortrait_ActingScale_Y));

        RectTransform newExtensionsRoot = FindByName(rigRoot, extensionRootName) as RectTransform;
        RectTransform extensionParent = FindByName(rigRoot, extensionParentName) as RectTransform;

        if (extensionParent == null)
        {
            Debug.LogWarning(
                $"[CharacterRigBuilder] Failed to find extension parent '{extensionParentName}'. " +
                $"Reattaching preserved extensions root under rigRoot. " +
                $"rigRoot='{rigRoot.name}'.", rigRoot);

            extensionParent = rigRoot;
        }

        int siblingIndex = -1;

        if (newExtensionsRoot != null && newExtensionsRoot != preservedExtensionsRoot)
        {
            siblingIndex = newExtensionsRoot.GetSiblingIndex();

            newExtensionsRoot.SetParent(null, false);
            Object.Destroy(newExtensionsRoot.gameObject);
        }

        preservedExtensionsRoot.name = extensionRootName;
        preservedExtensionsRoot.SetParent(extensionParent, false);

        if (siblingIndex >= 0)
            preservedExtensionsRoot.SetSiblingIndex(siblingIndex);

        StretchFull(preservedExtensionsRoot);
    }
    #endregion

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
        CharacterRigRefs refs = new(rigRoot);

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
        refs.CharSlot_DepthY = GetRt(CharacterRigSchema.Refs.CharSlot_DepthY);
        refs.CharSlot_Track = GetRt(CharacterRigSchema.Refs.CharSlot_Track);
        refs.CharSlot_Track_Focus = GetRt(CharacterRigSchema.Refs.CharSlot_Track_Focus);
        refs.CharSlot_Track_Idle = GetRt(CharacterRigSchema.Refs.CharSlot_Track_Idle);
        refs.CharSlot_Track_X = GetRt(CharacterRigSchema.Refs.CharSlot_Track_X);
        refs.CharSlot_Track_Y = GetRt(CharacterRigSchema.Refs.CharSlot_Track_Y);
        refs.CharSlot_Rotation = GetRt(CharacterRigSchema.Refs.CharSlot_Rotation);
        refs.CharSlot_SwayPivot = GetRt(CharacterRigSchema.Refs.CharSlot_SwayPivot);
        refs.CharSlot_DepthScale = GetRt(CharacterRigSchema.Refs.CharSlot_DepthScale);
        refs.CharSlot_Scale = GetRt(CharacterRigSchema.Refs.CharSlot_Scale);
        refs.CharSlot_Size = GetRt(CharacterRigSchema.Refs.CharSlot_Size);

        // Character casting axis - per-character defaults
        refs.Character_Root = GetRt(CharacterRigSchema.Refs.Character_Root);
        refs.Character_CastTransform = GetRt(CharacterRigSchema.Refs.Character_CastTransform);

        // Portrait acting axis
        refs.CharacterPortrait_Track = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track);
        refs.CharacterPortrait_Track_Move = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track_Move);
        refs.CharacterPortrait_Track_X = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track_X);
        refs.CharacterPortrait_Track_Y = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Track_Y);
        refs.CharacterPortrait_Rotation = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Rotation);
        refs.CharacterPortrait_SwayPivot = GetRt(CharacterRigSchema.Refs.CharacterPortrait_SwayPivot);
        refs.CharacterPortrait_Shake = GetRt(CharacterRigSchema.Refs.CharacterPortrait_Shake);

        // Emoji00 casting/effect axis
        refs.CharacterEmojiSlot00_Root = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_Root);
        refs.CharacterEmojiSlot00_CastTransform = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_CastTransform);
        
        // Emoji00 sprite motion / effect axis
        refs.EmojiSlot00_Track_Move = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Track_Move);
        refs.EmojiSlot00_Track_X = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Track_X);
        refs.EmojiSlot00_Track_Y = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Track_Y);
        refs.CharacterEmojiSlot00_Effect = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot00_Effect);
        refs.EmojiSlot00_BaseSize = GetRt(CharacterRigSchema.Refs.EmojiSlot00_BaseSize);
        refs.EmojiSlot00_Scale = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Scale);
        refs.EmojiSlot00_SwayPivot = GetRt(CharacterRigSchema.Refs.EmojiSlot00_SwayPivot);
        refs.EmojiSlot00_BaseRotation = GetRt(CharacterRigSchema.Refs.EmojiSlot00_BaseRotation);
        refs.EmojiSlot00_Rotation = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Rotation);
        refs.EmojiSlot00_Rotation_Offset = GetRt(CharacterRigSchema.Refs.EmojiSlot00_Rotation_Offset);
        refs.EmojiSlot00_Image = GetImg(CharacterRigSchema.Refs.EmojiSlot00_Image);
        
        // Emoji01 casting/effect axis
        refs.CharacterEmojiSlot01_Root = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_Root);
        refs.CharacterEmojiSlot01_CastTransform = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_CastTransform);
        
        // Emoji01 sprite motion / effect axis
        refs.EmojiSlot01_Track_Move = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Track_Move);
        refs.EmojiSlot01_Track_X = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Track_X);
        refs.EmojiSlot01_Track_Y = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Track_Y);
        refs.CharacterEmojiSlot01_Effect = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot01_Effect);
        refs.EmojiSlot01_BaseSize = GetRt(CharacterRigSchema.Refs.EmojiSlot01_BaseSize);
        refs.EmojiSlot01_Scale = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Scale);
        refs.EmojiSlot01_SwayPivot = GetRt(CharacterRigSchema.Refs.EmojiSlot01_SwayPivot);
        refs.EmojiSlot01_BaseRotation = GetRt(CharacterRigSchema.Refs.EmojiSlot01_BaseRotation);
        refs.EmojiSlot01_Rotation = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Rotation);
        refs.EmojiSlot01_Rotation_Offset = GetRt(CharacterRigSchema.Refs.EmojiSlot01_Rotation_Offset);
        refs.EmojiSlot01_Image = GetImg(CharacterRigSchema.Refs.EmojiSlot01_Image);
        
        // Emoji02 casting/effect axis
        refs.CharacterEmojiSlot02_Root = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_Root);
        refs.CharacterEmojiSlot02_CastTransform = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_CastTransform);
        
        // Emoji02 sprite motion / effect axis
        refs.EmojiSlot02_Track_Move = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Track_Move);
        refs.EmojiSlot02_Track_X = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Track_X);
        refs.EmojiSlot02_Track_Y = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Track_Y);
        refs.CharacterEmojiSlot02_Effect = GetRt(CharacterRigSchema.Refs.CharacterEmojiSlot02_Effect);
        refs.EmojiSlot02_BaseSize = GetRt(CharacterRigSchema.Refs.EmojiSlot02_BaseSize);
        refs.EmojiSlot02_Scale = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Scale);
        refs.EmojiSlot02_SwayPivot = GetRt(CharacterRigSchema.Refs.EmojiSlot02_SwayPivot);
        refs.EmojiSlot02_BaseRotation = GetRt(CharacterRigSchema.Refs.EmojiSlot02_BaseRotation);
        refs.EmojiSlot02_Rotation = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Rotation);
        refs.EmojiSlot02_Rotation_Offset = GetRt(CharacterRigSchema.Refs.EmojiSlot02_Rotation_Offset);
        refs.EmojiSlot02_Image = GetImg(CharacterRigSchema.Refs.EmojiSlot02_Image);
        
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