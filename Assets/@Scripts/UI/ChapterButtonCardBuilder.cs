using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class ChapterButtonCardBuilder
{
    public RectTransform BuildCardRigRoot(
        RectTransform rigPrefab = null,
        string rolePrefix = "",
        string rigRootName = "ChapterButtonCard")
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
            GameObject rootGo = new GameObject(WithRole(rolePrefix, rigRootName), typeof(RectTransform));
            rigRoot = (RectTransform)rootGo.transform;

            StretchFull(rigRoot);
            EnsureGraph(rigRoot, rolePrefix);
        }

        return rigRoot;
    }

    public void BindRefsFromRoot(
        RectTransform rigRoot,
        string rolePrefix,
        out ChapterButtonCardRefs refs)
    {
        Dictionary<ChapterButtonCardSchema.Refs, RectTransform> map =
            CollectRefMap(rigRoot, rolePrefix);

        EnsureValidGraphMap(rigRoot, rolePrefix, ref map);

        refs = BuildRefs(rigRoot, map);
    }

    private void EnsureValidGraphMap(
        RectTransform rigRoot,
        string rolePrefix,
        ref Dictionary<ChapterButtonCardSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(ChapterButtonCardSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[ChapterButtonCardBuilder] Invalid card graph. " +
            $"Rebuilding from ChapterButtonCardSchema. " +
            $"Prefab may be broken, or saved with another role prefix. " +
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
        string extensionRootName = WithRole(rolePrefix, nameof(ChapterButtonCardSchema.Refs.ExtensionsRoot));

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

        string extensionRootName = WithRole(rolePrefix, nameof(ChapterButtonCardSchema.Refs.ExtensionsRoot));
        string extensionParentName = WithRole(rolePrefix, nameof(ChapterButtonCardSchema.Refs.Card_Root));

        RectTransform newExtensionsRoot = FindByName(rigRoot, extensionRootName) as RectTransform;
        RectTransform extensionParent = FindByName(rigRoot, extensionParentName) as RectTransform;

        if (extensionParent == null)
        {
            Debug.LogWarning(
                $"[ChapterButtonCardBuilder] Failed to find extension parent '{extensionParentName}'. " +
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

    private void EnsureGraph(RectTransform root, string rolePrefix)
    {
        foreach (ChapterButtonCardSchema.NodeDef node in ChapterButtonCardSchema.Nodes)
            EnsureNode(root, rolePrefix, node);
    }

    private void EnsureNode(
        RectTransform root,
        string rolePrefix,
        ChapterButtonCardSchema.NodeDef node)
    {
        RectTransform parentRt = node.Parent.HasValue
            ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
            : root;

        if (parentRt == null)
        {
            Debug.LogWarning(
                $"[ChapterButtonCardBuilder] Missing parent for node '{node.Id}'. " +
                $"parent='{node.Parent}', rigRoot='{root.name}'.",
                root);

            parentRt = root;
        }

        RectTransform rt = EnsureRect(parentRt, WithRole(rolePrefix, node.Id.ToString()));

        if (node.NeedsCenterPivot)
            rt.pivot = new Vector2(0.5f, 0.5f);

        if (node.NeedsTopLeftPivot)
            rt.pivot = new Vector2(0f, 1f);

        if (node.NeedsBottomPivot)
            rt.pivot = new Vector2(0.5f, 0f);

        if (node.NeedsCanvasGroup)
        {
            if (!rt.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = node.InitialCanvasGroupAlpha;
        }

        if (node.NeedsImage)
        {
            if (!rt.TryGetComponent(out Image image))
                image = rt.gameObject.AddComponent<Image>();

            image.raycastTarget = false;
        }

        if (node.NeedsButton)
        {
            if (!rt.TryGetComponent(out Image image))
            {
                image = rt.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }

            image.raycastTarget = true;

            if (!rt.TryGetComponent(out Button button))
                button = rt.gameObject.AddComponent<Button>();

            button.transition = Selectable.Transition.None;
        }

        if (node.NeedsText)
        {
            if (!rt.TryGetComponent(out TMP_Text text))
            {
                TextMeshProUGUI created = rt.gameObject.AddComponent<TextMeshProUGUI>();
                created.raycastTarget = false;
                created.text = "";
                created.fontSize = 24f;
                created.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private RectTransform EnsureRect(RectTransform parent, string name)
    {
        RectTransform existing = FindByName(parent, name) as RectTransform;

        if (existing != null)
            return existing;

        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;

        rt.SetParent(parent, false);
        StretchFull(rt);

        return rt;
    }

    private Dictionary<ChapterButtonCardSchema.Refs, RectTransform> CollectRefMap(
        RectTransform rigRoot,
        string rolePrefix)
    {
        Dictionary<ChapterButtonCardSchema.Refs, RectTransform> map =
            new Dictionary<ChapterButtonCardSchema.Refs, RectTransform>();

        foreach (ChapterButtonCardSchema.Refs id in Enum.GetValues(typeof(ChapterButtonCardSchema.Refs)))
        {
            string nodeName = WithRole(rolePrefix, id.ToString());
            RectTransform t = FindByName(rigRoot, nodeName) as RectTransform;

            if (t != null)
                map[id] = t;
        }

        return map;
    }

    private ChapterButtonCardRefs BuildRefs(
        RectTransform rigRoot,
        Dictionary<ChapterButtonCardSchema.Refs, RectTransform> map)
    {
        ChapterButtonCardRefs refs = new ChapterButtonCardRefs(rigRoot);

        RectTransform GetRt(ChapterButtonCardSchema.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform targetRect) || targetRect == null)
            {
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing bound ref '{key}'.", rigRoot);
                return null;
            }

            return targetRect;
        }

        Image GetImage(ChapterButtonCardSchema.Refs key)
        {
            RectTransform rt = GetRt(key);

            if (rt == null)
                return null;

            Image image = rt.GetComponent<Image>();

            if (image == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing Image on '{rt.name}'.", rt);

            return image;
        }

        TMP_Text GetText(ChapterButtonCardSchema.Refs key)
        {
            RectTransform rt = GetRt(key);

            if (rt == null)
                return null;

            TMP_Text text = rt.GetComponent<TMP_Text>();

            if (text == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing TMP_Text on '{rt.name}'.", rt);

            return text;
        }

        Button GetButton(ChapterButtonCardSchema.Refs key)
        {
            RectTransform rt = GetRt(key);

            if (rt == null)
                return null;

            Button button = rt.GetComponent<Button>();

            if (button == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing Button on '{rt.name}'.", rt);

            return button;
        }

        CanvasGroup GetCanvasGroup(ChapterButtonCardSchema.Refs key)
        {
            RectTransform rt = GetRt(key);

            if (rt == null)
                return null;

            CanvasGroup canvasGroup = rt.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing CanvasGroup on '{rt.name}'.", rt);

            return canvasGroup;
        }

        refs.Card_Root = GetRt(ChapterButtonCardSchema.Refs.Card_Root);
        refs.Card_Root_CanvasGroup = GetCanvasGroup(ChapterButtonCardSchema.Refs.Card_Root);

        refs.Card_LayoutRoot = GetRt(ChapterButtonCardSchema.Refs.Card_LayoutRoot);
        refs.Card_MotionRoot = GetRt(ChapterButtonCardSchema.Refs.Card_MotionRoot);
        refs.Card_ShakeRoot = GetRt(ChapterButtonCardSchema.Refs.Card_ShakeRoot);
        refs.Card_ScaleRoot = GetRt(ChapterButtonCardSchema.Refs.Card_ScaleRoot);

        refs.Bg_Root = GetRt(ChapterButtonCardSchema.Refs.Bg_Root);
        refs.Bg_Pad = GetRt(ChapterButtonCardSchema.Refs.Bg_Pad);
        refs.Bg_Image = GetImage(ChapterButtonCardSchema.Refs.Bg_Image);

        refs.BgOverlay_Root = GetRt(ChapterButtonCardSchema.Refs.BgOverlay_Root);
        refs.BgOverlay_Pad = GetRt(ChapterButtonCardSchema.Refs.BgOverlay_Pad);
        refs.BgOverlay_Image = GetImage(ChapterButtonCardSchema.Refs.BgOverlay_Image);

        refs.Index_Root = GetRt(ChapterButtonCardSchema.Refs.Index_Root);
        refs.Index_Anchor = GetRt(ChapterButtonCardSchema.Refs.Index_Anchor);
        refs.Index_Text = GetText(ChapterButtonCardSchema.Refs.Index_Text);

        refs.HeadingBlock_Root = GetRt(ChapterButtonCardSchema.Refs.HeadingBlock_Root);

        refs.ChapterIndexLabel_Root = GetRt(ChapterButtonCardSchema.Refs.ChapterIndexLabel_Root);
        refs.ChapterIndexLabel_Image = GetImage(ChapterButtonCardSchema.Refs.ChapterIndexLabel_Image);
        refs.ChapterIndexLabel_Text = GetText(ChapterButtonCardSchema.Refs.ChapterIndexLabel_Text);

        refs.ChapterTitleLabel_Root = GetRt(ChapterButtonCardSchema.Refs.ChapterTitleLabel_Root);
        refs.ChapterTitleLabelBG_Image = GetImage(ChapterButtonCardSchema.Refs.ChapterTitleLabelBG_Image);
        refs.ChapterTitleLabelIcon_Image = GetImage(ChapterButtonCardSchema.Refs.ChapterTitleLabelIcon_Image);
        refs.ChapterTitleLabel_Text = GetText(ChapterButtonCardSchema.Refs.ChapterTitleLabel_Text);

        refs.EpisodeHeadingLabel_Root = GetRt(ChapterButtonCardSchema.Refs.EpisodeHeadingLabel_Root);
        refs.EpisodeHeadingLabel_Image = GetImage(ChapterButtonCardSchema.Refs.EpisodeHeadingLabel_Image);
        refs.EpisodeHeadingLabel_Text = GetText(ChapterButtonCardSchema.Refs.EpisodeHeadingLabel_Text);

        refs.Hit_Root = GetRt(ChapterButtonCardSchema.Refs.Hit_Root);
        refs.Hit_Button = GetButton(ChapterButtonCardSchema.Refs.Hit_Button);

        refs.Selected_Root = GetRt(ChapterButtonCardSchema.Refs.Selected_Root);
        refs.Selected_Root_CanvasGroup = GetCanvasGroup(ChapterButtonCardSchema.Refs.Selected_Root);

        refs.Locked_Root = GetRt(ChapterButtonCardSchema.Refs.Locked_Root);
        refs.Locked_Root_CanvasGroup = GetCanvasGroup(ChapterButtonCardSchema.Refs.Locked_Root);

        refs.ExtensionsRoot = GetRt(ChapterButtonCardSchema.Refs.ExtensionsRoot);

        return refs;
    }

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
}