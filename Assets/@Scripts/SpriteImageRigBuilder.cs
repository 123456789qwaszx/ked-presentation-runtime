using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class SpriteImageRigBuilder
{
    public RectTransform BuildSpriteImageRoot(
        RectTransform prefab = null,
        string rolePrefix = "",
        string rootName = "SpriteImage")
    {
        RectTransform root;

        if (prefab != null)
        {
            root = Object.Instantiate(prefab);
            root.name = WithRole(rolePrefix, rootName);

            if (!string.IsNullOrEmpty(rolePrefix))
                PrefixAllChildren(root.transform, rolePrefix);
        }
        else
        {
            GameObject go = new(WithRole(rolePrefix, rootName), typeof(RectTransform));
            root = (RectTransform)go.transform;
        }

        StretchFull(root);
        EnsureGraph(root, rolePrefix);

        return root;
    }

    public void BindRefsFromRoot(
        RectTransform root,
        string rolePrefix,
        out SpriteImageRigRefs refs)
    {
        Dictionary<SpriteImageRigSchema.Refs, RectTransform> map =
            CollectRefMap(root, rolePrefix);

        EnsureValidGraphMap(root, rolePrefix, ref map);

        refs = BuildRefs(root, map);
    }

    private void EnsureValidGraphMap(
        RectTransform root,
        string rolePrefix,
        ref Dictionary<SpriteImageRigSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(SpriteImageRigSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[SpriteImageBuilder] Invalid graph. " +
            $"Rebuilding from SpriteImageSchema. " +
            $"root='{root.name}', rolePrefix='{rolePrefix}'.",
            root);

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            child.SetParent(null, false);
            Object.Destroy(child.gameObject);
        }

        EnsureGraph(root, rolePrefix);
        map = CollectRefMap(root, rolePrefix);
    }

    private void EnsureGraph(RectTransform root, string rolePrefix)
    {
        foreach (SpriteImageRigSchema.NodeDef node in SpriteImageRigSchema.Nodes)
            EnsureNode(root, rolePrefix, node);

        NormalizeSiblingOrder(root, rolePrefix);
    }

    private void EnsureNode(
        RectTransform root,
        string rolePrefix,
        SpriteImageRigSchema.NodeDef node)
    {
        RectTransform parent = node.Parent.HasValue
            ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
            : root;

        if (parent == null)
            parent = root;

        RectTransform rt = EnsureRect(
            root,
            parent,
            WithRole(rolePrefix, node.Id.ToString()),
            node.StretchFull,
            node.NeedsBottomPivot,
            node.NeedsCenterPivot);

        if (node.NeedsCanvasGroup)
        {
            if (!rt.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = node.InitialCanvasGroupAlpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (node.NeedsImage)
        {
            if (!rt.TryGetComponent(out Image image))
                image = rt.gameObject.AddComponent<Image>();

            image.color = node.InitialImageColor;
            image.raycastTarget = node.RaycastTarget;
            image.preserveAspect = true;
        }
    }

    private RectTransform EnsureRect(
        RectTransform root,
        RectTransform parent,
        string name,
        bool stretchFull,
        bool bottomPivot,
        bool centerPivot)
    {
        RectTransform existing = FindByName(root, name) as RectTransform;
        bool created = existing == null;

        if (created)
        {
            GameObject go = new(name, typeof(RectTransform));
            existing = (RectTransform)go.transform;
        }

        if (existing.parent != parent)
            existing.SetParent(parent, false);

        if (stretchFull)
        {
            StretchFull(existing);
        }
        else if (created)
        {
            existing.anchorMin = new Vector2(0.5f, 0.5f);
            existing.anchorMax = new Vector2(0.5f, 0.5f);

            if (bottomPivot)
                existing.pivot = new Vector2(0.5f, 0f);
            else if (centerPivot)
                existing.pivot = new Vector2(0.5f, 0.5f);

            existing.anchoredPosition = Vector2.zero;
            existing.sizeDelta = Vector2.zero;
            existing.localScale = Vector3.one;
            existing.localRotation = Quaternion.identity;
        }

        return existing;
    }

    private Dictionary<SpriteImageRigSchema.Refs, RectTransform> CollectRefMap(
        RectTransform root,
        string rolePrefix)
    {
        Dictionary<SpriteImageRigSchema.Refs, RectTransform> map = new();

        foreach (SpriteImageRigSchema.Refs id in
                 Enum.GetValues(typeof(SpriteImageRigSchema.Refs)))
        {
            string nodeName = WithRole(rolePrefix, id.ToString());
            Transform t = FindByName(root, nodeName);

            if (t is RectTransform rt)
                map[id] = rt;
        }

        return map;
    }

    private SpriteImageRigRefs BuildRefs(
        RectTransform root,
        Dictionary<SpriteImageRigSchema.Refs, RectTransform> map)
    {
        SpriteImageRigRefs refs = new(root);

        RectTransform GetRt(SpriteImageRigSchema.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform rt) || rt == null)
            {
                Debug.LogWarning($"[SpriteImageBuilder] Missing ref '{key}'.");
                return null;
            }

            return rt;
        }

        Image GetImg(SpriteImageRigSchema.Refs key)
        {
            RectTransform rt = GetRt(key);
            return rt != null ? rt.GetComponent<Image>() : null;
        }

        refs.Sprite_Root = GetRt(SpriteImageRigSchema.Refs.Sprite_Root);
        refs.Sprite_RootCanvasGroup = refs.Sprite_Root != null
            ? refs.Sprite_Root.GetComponent<CanvasGroup>()
            : null;

        refs.Sprite_Anchor = GetRt(SpriteImageRigSchema.Refs.Sprite_Anchor);

        refs.Sprite_BaseRotation = GetRt(SpriteImageRigSchema.Refs.Sprite_BaseRotation);

        refs.Sprite_Track_Move = GetRt(SpriteImageRigSchema.Refs.Sprite_Track_Move);
        refs.Sprite_Track_X = GetRt(SpriteImageRigSchema.Refs.Sprite_Track_X);
        refs.Sprite_Track_X_Offset = GetRt(SpriteImageRigSchema.Refs.Sprite_Track_X_Offset);
        refs.Sprite_Track_Y = GetRt(SpriteImageRigSchema.Refs.Sprite_Track_Y);
        refs.Sprite_Track_Y_Offset = GetRt(SpriteImageRigSchema.Refs.Sprite_Track_Y_Offset);

        refs.Sprite_Rotation = GetRt(SpriteImageRigSchema.Refs.Sprite_Rotation);

        refs.Sprite_Size = GetRt(SpriteImageRigSchema.Refs.Sprite_Size);
        refs.Sprite_Scale = GetRt(SpriteImageRigSchema.Refs.Sprite_Scale);

        refs.Sprite_ActingScale = GetRt(SpriteImageRigSchema.Refs.Sprite_ActingScale);
        refs.Sprite_ActingScale_X = GetRt(SpriteImageRigSchema.Refs.Sprite_ActingScale_X);
        refs.Sprite_ActingScale_Y = GetRt(SpriteImageRigSchema.Refs.Sprite_ActingScale_Y);

        refs.Sprite_Image = GetImg(SpriteImageRigSchema.Refs.Sprite_Image);

        return refs;
    }

    private void NormalizeSiblingOrder(RectTransform root, string rolePrefix)
    {
        Dictionary<Transform, int> nextIndexByParent = new();

        foreach (SpriteImageRigSchema.NodeDef node in SpriteImageRigSchema.Nodes)
        {
            RectTransform rt =
                FindByName(root, WithRole(rolePrefix, node.Id.ToString())) as RectTransform;

            if (rt == null)
                continue;

            RectTransform parent = node.Parent.HasValue
                ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
                : root;

            if (parent == null)
                continue;

            int index = nextIndexByParent.TryGetValue(parent, out int current)
                ? current
                : 0;

            if (rt.parent == parent)
                rt.SetSiblingIndex(index);

            nextIndexByParent[parent] = index + 1;
        }
    }

    private Transform FindByName(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindByName(root.GetChild(i), name);

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

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private string WithRole(string rolePrefix, string baseName)
    {
        if (string.IsNullOrEmpty(rolePrefix))
            return baseName;

        if (baseName.StartsWith(rolePrefix, StringComparison.Ordinal))
            return baseName;

        return $"{rolePrefix}{baseName}";
    }
}