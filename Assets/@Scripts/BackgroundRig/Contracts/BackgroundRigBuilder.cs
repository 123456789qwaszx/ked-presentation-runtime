using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BackgroundRigBuilder
{
    public RectTransform BuildBackgroundRigRoot(
        RectTransform rigPrefab = null,
        string rolePrefix = "",
        string rigRootName = "BackgroundRig")
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

    public void BindRefsFromRoot(RectTransform rigRoot, string rolePrefix, out BackgroundRigRefs refs)
    {
        Dictionary<BackgroundRigSchema.Refs, RectTransform> map = CollectRefMap(rigRoot, rolePrefix);
        EnsureValidGraphMap(rigRoot, rolePrefix, ref map);

        refs = BuildRefs(rigRoot, map);
    }

    #region Graph Recovery
    private void EnsureValidGraphMap(
        RectTransform rigRoot,
        string rolePrefix,
        ref Dictionary<BackgroundRigSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(BackgroundRigSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[BackgroundRigBuilder] Invalid rig graph. " +
            $"Rebuilding from BackgroundRigSchema. " +
            $"Prefab may be broken, or saved with another role prefix. " +
            $"For reusable prefab baking, call StripRolePrefixForBake after refs registration before saving. " +
            $"rigRoot='{rigRoot.name}', rolePrefix='{rolePrefix}'.",
            rigRoot);

        RectTransform preservedExtensionsRoot = DetachPreservedExtensionsRoot(rigRoot, rolePrefix);

        for (int i = rigRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rigRoot.GetChild(i);
            child.SetParent(null, false);
            Object.Destroy(child.gameObject);
        }

        EnsureGraph(rigRoot, rolePrefix);

        ReattachPreservedExtensionsRoot(rigRoot, rolePrefix, preservedExtensionsRoot);

        map = CollectRefMap(rigRoot, rolePrefix);
    }

    private RectTransform DetachPreservedExtensionsRoot(RectTransform rigRoot, string rolePrefix)
    {
        string extensionRootName = WithRole(rolePrefix, nameof(BackgroundRigSchema.Refs.Background_ExtensionsRoot));

        RectTransform extensionsRoot = FindByName(rigRoot, extensionRootName) as RectTransform;

        if (extensionsRoot == null)
            return null;

        extensionsRoot.SetParent(null, false);

        return extensionsRoot;
    }

    private void ReattachPreservedExtensionsRoot(
        RectTransform rigRoot,
        string rolePrefix,
        RectTransform preservedExtensionsRoot)
    {
        if (preservedExtensionsRoot == null)
            return;

        string extensionRootName = WithRole(rolePrefix, nameof(BackgroundRigSchema.Refs.Background_ExtensionsRoot));
        string extensionParentName = WithRole(rolePrefix, nameof(BackgroundRigSchema.Refs.Background_LayerRoot));

        RectTransform newExtensionsRoot = FindByName(rigRoot, extensionRootName) as RectTransform;
        RectTransform extensionParent = FindByName(rigRoot, extensionParentName) as RectTransform;

        if (extensionParent == null)
        {
            Debug.LogWarning(
                $"[BackgroundRigBuilder] Failed to find extension parent '{extensionParentName}'. " +
                $"Reattaching preserved extensions root under rigRoot. " +
                $"rigRoot='{rigRoot.name}'.",
                rigRoot);

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
        foreach (BackgroundRigSchema.NodeDef node in BackgroundRigSchema.Nodes)
            EnsureNode(root, rolePrefix, node);
    }

    private void EnsureNode(RectTransform root, string rolePrefix, BackgroundRigSchema.NodeDef node)
    {
        RectTransform parentRt = node.Parent.HasValue
            ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
            : root;

        RectTransform rt = EnsureRect(parentRt, WithRole(rolePrefix, node.Id.ToString()));

        if (node.NeedsCenterPivot)
            rt.pivot = new Vector2(0.5f, 0.5f);

        if (node.NeedsBottomPivot)
            rt.pivot = new Vector2(0.5f, 0f);

        if (node.NeedsCanvasGroup)
        {
            if (!rt.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup))
                canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = node.InitialCanvasGroupAlpha;
        }

        if (node.NeedsImage && !rt.TryGetComponent<Image>(out _))
        {
            Image image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
        }

        if (node.NeedsRawImage && !rt.TryGetComponent<RawImage>(out _))
        {
            RawImage rawImage = rt.gameObject.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
        }
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
    private Dictionary<BackgroundRigSchema.Refs, RectTransform> CollectRefMap(
        RectTransform rigRoot,
        string rolePrefix)
    {
        Dictionary<BackgroundRigSchema.Refs, RectTransform> map = new();

        foreach (BackgroundRigSchema.Refs id in Enum.GetValues(typeof(BackgroundRigSchema.Refs)))
        {
            string nodeName = WithRole(rolePrefix, id.ToString());
            RectTransform t = FindByName(rigRoot, nodeName) as RectTransform;

            if (t != null)
                map[id] = t;
        }

        return map;
    }

    private BackgroundRigRefs BuildRefs(
        RectTransform rigRoot,
        Dictionary<BackgroundRigSchema.Refs, RectTransform> map)
    {
        BackgroundRigRefs refs = new(rigRoot);

        RectTransform GetRt(BackgroundRigSchema.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform targetRect) || targetRect == null)
            {
                Debug.LogWarning($"[BackgroundRigBuilder] Missing bound ref '{key}'.");
                return null;
            }

            return targetRect;
        }

        Image GetImg(BackgroundRigSchema.Refs key)
        {
            RectTransform rt = GetRt(key);
            if (rt == null)
                return null;

            Image img = rt.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning($"[BackgroundRigBuilder] Missing Image on '{rt.name}'.");
                return null;
            }

            return img;
        }

        RawImage GetRawImg(BackgroundRigSchema.Refs key)
        {
            RectTransform rt = GetRt(key);
            if (rt == null)
                return null;

            RawImage rawImg = rt.GetComponent<RawImage>();
            if (rawImg == null)
            {
                Debug.LogWarning($"[BackgroundRigBuilder] Missing RawImage on '{rt.name}'.");
                return null;
            }

            return rawImg;
        }

        // Background base axis - response-neutral placement / measurement
        refs.Background_Root = GetRt(BackgroundRigSchema.Refs.Background_Root);

        // Framing axis - pseudo camera / focus response
        refs.Background_FramingTransform = GetRt(BackgroundRigSchema.Refs.Background_FramingTransform);
        refs.Background_FramingScale = GetRt(BackgroundRigSchema.Refs.Background_FramingScale);

        // Background casting axis - per-background defaults
        refs.Background_CastTransform = GetRt(BackgroundRigSchema.Refs.Background_CastTransform);

        // Background acting axis
        refs.Background_Track = GetRt(BackgroundRigSchema.Refs.Background_Track);
        refs.Background_Track_Move = GetRt(BackgroundRigSchema.Refs.Background_Track_Move);
        refs.Background_Track_X = GetRt(BackgroundRigSchema.Refs.Background_Track_X);
        refs.Background_Track_Y = GetRt(BackgroundRigSchema.Refs.Background_Track_Y);
        refs.Background_Rotation = GetRt(BackgroundRigSchema.Refs.Background_Rotation);
        refs.Background_Shake = GetRt(BackgroundRigSchema.Refs.Background_Shake);
        refs.Background_ActingScale = GetRt(BackgroundRigSchema.Refs.Background_ActingScale);
        refs.Background_ActingScale_X = GetRt(BackgroundRigSchema.Refs.Background_ActingScale_X);
        refs.Background_ActingScale_Y = GetRt(BackgroundRigSchema.Refs.Background_ActingScale_Y);

        // Layer stack
        refs.Background_LayerRoot = GetRt(BackgroundRigSchema.Refs.Background_LayerRoot);

        // Back layer
        refs.Background_BackLayer_Root = GetRt(BackgroundRigSchema.Refs.Background_BackLayer_Root);
        refs.Background_BackLayer_Image = GetImg(BackgroundRigSchema.Refs.Background_BackLayer_Image);

        // Object slots
        refs.Background_ObjectSlotRoot = GetRt(BackgroundRigSchema.Refs.Background_ObjectSlotRoot);
        refs.Background_ObjectSlot00 = GetRt(BackgroundRigSchema.Refs.Background_ObjectSlot00);
        refs.Background_ObjectSlot01 = GetRt(BackgroundRigSchema.Refs.Background_ObjectSlot01);
        refs.Background_ObjectSlot02 = GetRt(BackgroundRigSchema.Refs.Background_ObjectSlot02);

        // Front layer
        refs.Background_FrontLayer_Root = GetRt(BackgroundRigSchema.Refs.Background_FrontLayer_Root);
        refs.Background_FrontLayer_Image = GetImg(BackgroundRigSchema.Refs.Background_FrontLayer_Image);

        // Defocus overlay
        refs.Background_DefocusOverlay_Root = GetRt(BackgroundRigSchema.Refs.Background_DefocusOverlay_Root);
        refs.Background_DefocusOverlay_RawImage = GetRawImg(BackgroundRigSchema.Refs.Background_DefocusOverlay_RawImage);

        // Extension / preserved systems
        refs.Background_ExtensionsRoot = GetRt(BackgroundRigSchema.Refs.Background_ExtensionsRoot);

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