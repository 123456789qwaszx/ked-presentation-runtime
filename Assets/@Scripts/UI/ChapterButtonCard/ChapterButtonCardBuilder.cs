using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class ChapterButtonCardBuilder
{
    public ChapterButtonCard BuildCard(
        RectTransform parent,
        RectTransform prefab = null,
        string rolePrefix = "",
        string rootName = "ChapterButtonCard",
        ChapterButtonCardBuildOptions options = null)
    {
        ChapterButtonCardBuildOptions resolvedOptions = options ?? new ChapterButtonCardBuildOptions();

        RectTransform root = CreateRoot(prefab, rolePrefix, rootName);

        if (parent != null)
            root.SetParent(parent, false);
        else
            Debug.LogWarning("[ChapterButtonCardBuilder] Parent is null. Card will be created without parent.", root);

        ChapterButtonCard card = root.GetComponent<ChapterButtonCard>();

        if (card == null)
            card = root.gameObject.AddComponent<ChapterButtonCard>();

        // 이미 완성된 prefab이면, 크기/스타일/참조를 덮어쓰지 않는다.
        if (card.HasRequiredReferences())
            return card;

        ApplyCardRootDefaults(root, resolvedOptions);

        Dictionary<ChapterButtonCardSchema.Node, RectTransform> map = EnsureGraph(root, rolePrefix);
        
        ChapterButtonCard.References generatedRefs = BuildReferences(root, map, rolePrefix);

        ApplyDefaultStyle(generatedRefs, resolvedOptions);

        card.AssignGeneratedReferences(generatedRefs);

        return card;
    }

    private RectTransform CreateRoot(
        RectTransform prefab,
        string rolePrefix,
        string rootName)
    {
        if (prefab != null)
        {
            RectTransform instance = Object.Instantiate(prefab);
            instance.name = WithRole(rolePrefix, rootName);

            // 완성 prefab을 존중하기 위해 여기서 PrefixAllChildren을 하지 않는다.
            // 필요한 경우 FindNode에서 prefixed/raw 이름을 모두 탐색한다.
            return instance;
        }

        GameObject rootGo = new GameObject(WithRole(rolePrefix, rootName), typeof(RectTransform));
        RectTransform root = (RectTransform)rootGo.transform;

        StretchFull(root);

        return root;
    }

    private Dictionary<ChapterButtonCardSchema.Node, RectTransform> EnsureGraph(
        RectTransform root,
        string rolePrefix)
    {
        Dictionary<ChapterButtonCardSchema.Node, RectTransform> map =
            new Dictionary<ChapterButtonCardSchema.Node, RectTransform>();

        for (int i = 0; i < ChapterButtonCardSchema.Nodes.Length; i++)
        {
            ChapterButtonCardSchema.NodeDef node = ChapterButtonCardSchema.Nodes[i];
            RectTransform rect = EnsureNode(root, rolePrefix, node, map);

            if (rect != null)
                map[node.Id] = rect;
        }

        return map;
    }

    private RectTransform EnsureNode(
        RectTransform root,
        string rolePrefix,
        ChapterButtonCardSchema.NodeDef node,
        Dictionary<ChapterButtonCardSchema.Node, RectTransform> map)
    {
        RectTransform parent = ResolveParent(root, rolePrefix, node, map);

        string nodeName = WithRole(rolePrefix, node.Id.ToString());
        RectTransform rect = FindNode(root, rolePrefix, node.Id);

        if (rect == null)
        {
            GameObject go = new GameObject(nodeName, typeof(RectTransform));
            rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            StretchFull(rect);
        }
        else if (rect.parent == null)
        {
            rect.SetParent(parent, false);
        }

        ApplyNodeOptions(rect, node);

        return rect;
    }

    private RectTransform ResolveParent(
        RectTransform root,
        string rolePrefix,
        ChapterButtonCardSchema.NodeDef node,
        Dictionary<ChapterButtonCardSchema.Node, RectTransform> map)
    {
        if (!node.Parent.HasValue)
            return root;

        ChapterButtonCardSchema.Node parentNode = node.Parent.Value;

        if (map.TryGetValue(parentNode, out RectTransform mappedParent) && mappedParent != null)
            return mappedParent;

        RectTransform foundParent = FindNode(root, rolePrefix, parentNode);

        if (foundParent != null)
            return foundParent;

        Debug.LogWarning(
            $"[ChapterButtonCardBuilder] Missing parent node. " +
            $"node='{node.Id}', parent='{parentNode}', root='{root.name}'. Fallback to root.",
            root);

        return root;
    }

    private void ApplyNodeOptions(
        RectTransform rect,
        ChapterButtonCardSchema.NodeDef node)
    {
        if (rect == null || node == null)
            return;

        if (node.NeedsCenterPivot)
            rect.pivot = new Vector2(0.5f, 0.5f);

        if (node.NeedsTopLeftPivot)
            rect.pivot = new Vector2(0f, 1f);

        if (node.NeedsBottomPivot)
            rect.pivot = new Vector2(0.5f, 0f);

        if (node.NeedsCanvasGroup)
        {
            CanvasGroup canvasGroup = GetOrAdd<CanvasGroup>(rect);
            canvasGroup.alpha = node.InitialCanvasGroupAlpha;
        }

        if (node.NeedsImage)
        {
            Image image = GetOrAdd<Image>(rect);
            image.raycastTarget = false;
        }

        if (node.NeedsButton)
        {
            Image image = GetOrAdd<Image>(rect);
            image.raycastTarget = true;
            image.color = new Color(1f, 1f, 1f, 0f);

            Button button = GetOrAdd<Button>(rect);
            button.transition = Selectable.Transition.None;
        }

        if (node.NeedsText)
        {
            TMP_Text text = rect.GetComponent<TMP_Text>();

            if (text == null)
            {
                TextMeshProUGUI created = rect.gameObject.AddComponent<TextMeshProUGUI>();
                created.raycastTarget = false;
                created.text = "";
                created.fontSize = 24f;
                created.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                text.raycastTarget = false;
            }
        }
    }

    private ChapterButtonCard.References BuildReferences(
        RectTransform root,
        Dictionary<ChapterButtonCardSchema.Node, RectTransform> map,
        string rolePrefix)
    {
        ChapterButtonCard.References refs = new ChapterButtonCard.References();

        RectTransform GetRect(ChapterButtonCardSchema.Node node)
        {
            if (map.TryGetValue(node, out RectTransform rect) && rect != null)
                return rect;

            RectTransform found = FindNode(root, rolePrefix, node);

            if (found != null)
                return found;

            Debug.LogWarning($"[ChapterButtonCardBuilder] Missing node ref '{node}'.", root);
            return null;
        }

        CanvasGroup GetCanvasGroup(ChapterButtonCardSchema.Node node)
        {
            RectTransform rect = GetRect(node);

            if (rect == null)
                return null;

            CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing CanvasGroup on '{rect.name}'.", rect);

            return canvasGroup;
        }

        Image GetImage(ChapterButtonCardSchema.Node node)
        {
            RectTransform rect = GetRect(node);

            if (rect == null)
                return null;

            Image image = rect.GetComponent<Image>();

            if (image == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing Image on '{rect.name}'.", rect);

            return image;
        }

        TMP_Text GetText(ChapterButtonCardSchema.Node node)
        {
            RectTransform rect = GetRect(node);

            if (rect == null)
                return null;

            TMP_Text text = rect.GetComponent<TMP_Text>();

            if (text == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing TMP_Text on '{rect.name}'.", rect);

            return text;
        }

        Button GetButton(ChapterButtonCardSchema.Node node)
        {
            RectTransform rect = GetRect(node);

            if (rect == null)
                return null;

            Button button = rect.GetComponent<Button>();

            if (button == null)
                Debug.LogWarning($"[ChapterButtonCardBuilder] Missing Button on '{rect.name}'.", rect);

            return button;
        }

        refs.cardRoot = GetRect(ChapterButtonCardSchema.Node.Card_Root);
        refs.cardCanvasGroup = GetCanvasGroup(ChapterButtonCardSchema.Node.Card_Root);

        refs.layoutRoot = GetRect(ChapterButtonCardSchema.Node.Card_LayoutRoot);
        refs.motionRoot = GetRect(ChapterButtonCardSchema.Node.Card_MotionRoot);
        refs.shakeRoot = GetRect(ChapterButtonCardSchema.Node.Card_ShakeRoot);
        refs.scaleRoot = GetRect(ChapterButtonCardSchema.Node.Card_ScaleRoot);

        refs.bgRoot = GetRect(ChapterButtonCardSchema.Node.Bg_Root);
        refs.bgPad = GetRect(ChapterButtonCardSchema.Node.Bg_Pad);
        refs.bgImage = GetImage(ChapterButtonCardSchema.Node.Bg_Image);

        refs.bgOverlayRoot = GetRect(ChapterButtonCardSchema.Node.BgOverlay_Root);
        refs.bgOverlayPad = GetRect(ChapterButtonCardSchema.Node.BgOverlay_Pad);
        refs.bgOverlayImage = GetImage(ChapterButtonCardSchema.Node.BgOverlay_Image);

        refs.indexRoot = GetRect(ChapterButtonCardSchema.Node.Index_Root);
        refs.indexAnchor = GetRect(ChapterButtonCardSchema.Node.Index_Anchor);
        refs.indexText = GetText(ChapterButtonCardSchema.Node.Index_Text);

        refs.headingBlockRoot = GetRect(ChapterButtonCardSchema.Node.HeadingBlock_Root);

        refs.chapterIndexLabelRoot = GetRect(ChapterButtonCardSchema.Node.ChapterIndexLabel_Root);
        refs.chapterIndexLabelImage = GetImage(ChapterButtonCardSchema.Node.ChapterIndexLabel_Image);
        refs.chapterIndexLabelText = GetText(ChapterButtonCardSchema.Node.ChapterIndexLabel_Text);

        refs.chapterTitleLabelRoot = GetRect(ChapterButtonCardSchema.Node.ChapterTitleLabel_Root);
        refs.chapterTitleLabelBgImage = GetImage(ChapterButtonCardSchema.Node.ChapterTitleLabelBG_Image);
        refs.chapterTitleLabelIconImage = GetImage(ChapterButtonCardSchema.Node.ChapterTitleLabelIcon_Image);
        refs.chapterTitleLabelText = GetText(ChapterButtonCardSchema.Node.ChapterTitleLabel_Text);

        refs.episodeHeadingLabelRoot = GetRect(ChapterButtonCardSchema.Node.EpisodeHeadingLabel_Root);
        refs.episodeHeadingLabelImage = GetImage(ChapterButtonCardSchema.Node.EpisodeHeadingLabel_Image);
        refs.episodeHeadingLabelText = GetText(ChapterButtonCardSchema.Node.EpisodeHeadingLabel_Text);

        refs.hitRoot = GetRect(ChapterButtonCardSchema.Node.Hit_Root);
        refs.hitButton = GetButton(ChapterButtonCardSchema.Node.Hit_Button);

        refs.selectedRoot = GetRect(ChapterButtonCardSchema.Node.Selected_Root);
        refs.selectedCanvasGroup = GetCanvasGroup(ChapterButtonCardSchema.Node.Selected_Root);

        refs.lockedRoot = GetRect(ChapterButtonCardSchema.Node.Locked_Root);
        refs.lockedCanvasGroup = GetCanvasGroup(ChapterButtonCardSchema.Node.Locked_Root);

        refs.extensionsRoot = GetRect(ChapterButtonCardSchema.Node.ExtensionsRoot);

        return refs;
    }

    private void ApplyCardRootDefaults(
        RectTransform root,
        ChapterButtonCardBuildOptions options)
    {
        if (root == null || options == null)
            return;

        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = options.defaultCardSize;

        if (!root.TryGetComponent(out LayoutElement layoutElement))
            layoutElement = root.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = options.defaultCardSize.x;
        layoutElement.preferredHeight = options.defaultCardSize.y;
        layoutElement.minWidth = options.defaultCardSize.x;
        layoutElement.minHeight = options.defaultCardSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void ApplyDefaultStyle(
        ChapterButtonCard.References refs,
        ChapterButtonCardBuildOptions options)
    {
        if (options == null)
            return;

        if (refs.bgImage != null && options.defaultBgSprite != null)
            refs.bgImage.sprite = options.defaultBgSprite;

        if (refs.bgOverlayImage != null && options.defaultBgOverlaySprite != null)
            refs.bgOverlayImage.sprite = options.defaultBgOverlaySprite;

        if (refs.chapterIndexLabelImage != null && options.defaultChapterIndexLabelSprite != null)
            refs.chapterIndexLabelImage.sprite = options.defaultChapterIndexLabelSprite;

        if (refs.episodeHeadingLabelImage != null && options.defaultEpisodeHeadingLabelSprite != null)
            refs.episodeHeadingLabelImage.sprite = options.defaultEpisodeHeadingLabelSprite;

        if (refs.chapterTitleLabelIconImage != null && options.defaultTitleIconSprite != null)
            refs.chapterTitleLabelIconImage.sprite = options.defaultTitleIconSprite;

        if (refs.indexText != null)
            refs.indexText.fontSize = options.indexFontSize;

        if (refs.chapterIndexLabelText != null)
            refs.chapterIndexLabelText.fontSize = options.chapterIndexFontSize;

        if (refs.chapterTitleLabelText != null)
            refs.chapterTitleLabelText.fontSize = options.titleFontSize;

        if (refs.episodeHeadingLabelText != null)
            refs.episodeHeadingLabelText.fontSize = options.episodeHeadingFontSize;

        if (refs.selectedCanvasGroup != null && options.hideSelectedByDefault)
        {
            refs.selectedCanvasGroup.alpha = 0f;
            refs.selectedCanvasGroup.interactable = false;
            refs.selectedCanvasGroup.blocksRaycasts = false;
        }

        if (refs.lockedCanvasGroup != null && options.hideLockedByDefault)
        {
            refs.lockedCanvasGroup.alpha = 0f;
            refs.lockedCanvasGroup.interactable = false;
            refs.lockedCanvasGroup.blocksRaycasts = false;
        }
    }

    private RectTransform FindNode(
        RectTransform root,
        string rolePrefix,
        ChapterButtonCardSchema.Node node)
    {
        if (root == null)
            return null;

        string rawName = node.ToString();
        string prefixedName = WithRole(rolePrefix, rawName);

        Transform prefixed = FindByName(root, prefixedName);

        if (prefixed != null)
            return prefixed as RectTransform;

        if (!string.Equals(prefixedName, rawName, StringComparison.Ordinal))
        {
            Transform raw = FindByName(root, rawName);

            if (raw != null)
                return raw as RectTransform;
        }

        return null;
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

    private T GetOrAdd<T>(RectTransform rect)
        where T : Component
    {
        if (rect.TryGetComponent(out T component))
            return component;

        return rect.gameObject.AddComponent<T>();
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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