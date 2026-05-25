using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class EpisodeNodeRigBuilder
{
    public RectTransform BuildNodeRigRoot(
        RectTransform rigPrefab = null,
        string nodePrefix = "",
        string rigRootName = "EpisodeNodeRig")
    {
        RectTransform rigRoot;

        if (rigPrefab != null)
        {
            rigRoot = Object.Instantiate(rigPrefab);
            rigRoot.name = WithPrefix(nodePrefix, rigRootName);

            if (!string.IsNullOrEmpty(nodePrefix))
                PrefixAllChildren(rigRoot.transform, nodePrefix);
        }
        else
        {
            GameObject rootGo = new GameObject(
                WithPrefix(nodePrefix, rigRootName),
                typeof(RectTransform));

            rigRoot = (RectTransform)rootGo.transform;
            StretchFull(rigRoot);
            EnsureGraph(rigRoot, nodePrefix);
        }

        return rigRoot;
    }

    public void BindRefsFromRoot(
        RectTransform rigRoot,
        string nodePrefix,
        out EpisodeNodeRigRefs refs)
    {
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map =
            CollectRefMap(rigRoot, nodePrefix);

        EnsureValidGraphMap(rigRoot, nodePrefix, ref map);

        refs = BuildRefs(rigRoot, map);
    }

    private void EnsureValidGraphMap(
        RectTransform rigRoot,
        string nodePrefix,
        ref Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(EpisodeNodeRigSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[EpisodeNodeRigBuilder] Invalid episode node rig. Rebuilding from schema. " +
            $"rigRoot='{rigRoot.name}', prefix='{nodePrefix}'.",
            rigRoot);

        for (int i = rigRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rigRoot.GetChild(i);

            child.SetParent(null, false);
            Object.Destroy(child.gameObject);
        }

        EnsureGraph(rigRoot, nodePrefix);
        map = CollectRefMap(rigRoot, nodePrefix);
    }

    private void EnsureGraph(RectTransform root, string nodePrefix)
    {
        foreach (EpisodeNodeRigSchema.NodeDef node in EpisodeNodeRigSchema.Nodes)
            EnsureNode(root, nodePrefix, node);
    }

    private void EnsureNode(
        RectTransform root,
        string nodePrefix,
        EpisodeNodeRigSchema.NodeDef node)
    {
        RectTransform parent = node.Parent.HasValue
            ? FindByName(root, WithPrefix(nodePrefix, node.Parent.Value.ToString())) as RectTransform
            : root;

        if (parent == null)
        {
            Debug.LogWarning(
                $"[EpisodeNodeRigBuilder] Parent not found. node='{node.Id}', parent='{node.Parent}'.",
                root);
            return;
        }

        RectTransform rt = EnsureRect(parent, WithPrefix(nodePrefix, node.Id.ToString()));

        if (node.NeedsCanvasGroup)
        {
            if (!rt.TryGetComponent(out CanvasGroup group))
                group = rt.gameObject.AddComponent<CanvasGroup>();

            group.alpha = node.InitialCanvasGroupAlpha;
            group.interactable = node.InitialCanvasGroupAlpha > 0f;
            group.blocksRaycasts = node.InitialCanvasGroupAlpha > 0f;
        }

        if (node.NeedsImage)
        {
            if (!rt.TryGetComponent<Image>(out _))
                rt.gameObject.AddComponent<Image>();
        }

        if (node.NeedsButton)
        {
            Image raycastImage = rt.GetComponent<Image>();

            if (raycastImage == null)
            {
                raycastImage = rt.gameObject.AddComponent<Image>();
                raycastImage.color = new Color(1f, 1f, 1f, 0f);
            }

            raycastImage.raycastTarget = true;

            if (!rt.TryGetComponent<Button>(out Button button))
                button = rt.gameObject.AddComponent<Button>();

            if (button.targetGraphic == null)
                button.targetGraphic = raycastImage;
        }

        if (node.NeedsText)
        {
            if (!rt.TryGetComponent<TMP_Text>(out _))
                rt.gameObject.AddComponent<TextMeshProUGUI>();
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

    private Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> CollectRefMap(
        RectTransform rigRoot,
        string nodePrefix)
    {
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map = new();

        foreach (EpisodeNodeRigSchema.Refs id in Enum.GetValues(typeof(EpisodeNodeRigSchema.Refs)))
        {
            RectTransform target = FindBySchemaOrLegacyName(rigRoot, nodePrefix, id);

            if (target != null)
                map[id] = target;
        }

        return map;
    }

    private RectTransform FindBySchemaOrLegacyName(
        RectTransform rigRoot,
        string nodePrefix,
        EpisodeNodeRigSchema.Refs id)
    {
        string currentName = WithPrefix(nodePrefix, id.ToString());
        Transform found = FindByName(rigRoot, currentName);

        if (found != null)
            return found as RectTransform;

        string legacyName = GetLegacyName(id);

        if (string.IsNullOrEmpty(legacyName))
            return null;

        string prefixedLegacyName = WithPrefix(nodePrefix, legacyName);
        found = FindByName(rigRoot, prefixedLegacyName);

        return found as RectTransform;
    }

    private static string GetLegacyName(EpisodeNodeRigSchema.Refs id)
    {
        switch (id)
        {
            case EpisodeNodeRigSchema.Refs.UpperLink_Root:
                return "UpperAttachment_Root";

            case EpisodeNodeRigSchema.Refs.UpperLinkBG_Image:
                return "UpperAttachmentBG_Image";

            case EpisodeNodeRigSchema.Refs.UpperLinkTitle_Root:
                return "UpperAttachmentTitle_Root";

            case EpisodeNodeRigSchema.Refs.UpperLinkTitle_Text:
                return "UpperAttachmentTitle_Text";

            case EpisodeNodeRigSchema.Refs.UpperLinkHit_Button:
                return "UpperAttachmentHit_Button";

            case EpisodeNodeRigSchema.Refs.LowerLink_Root:
                return "LowerAttachment_Root";

            case EpisodeNodeRigSchema.Refs.LowerLinkBG_Image:
                return "LowerAttachmentBG_Image";

            case EpisodeNodeRigSchema.Refs.LowerLinkTitle_Root:
                return "LowerAttachmentTitle_Root";

            case EpisodeNodeRigSchema.Refs.LowerLinkTitle_Text:
                return "LowerAttachmentTitle_Text";

            case EpisodeNodeRigSchema.Refs.LowerLinkHit_Button:
                return "LowerAttachmentHit_Button";

            default:
                return "";
        }
    }

    private EpisodeNodeRigRefs BuildRefs(
        RectTransform rigRoot,
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map)
    {
        EpisodeNodeRigRefs refs = new EpisodeNodeRigRefs(rigRoot);

        refs.NodeRoot = GetRt(map, EpisodeNodeRigSchema.Refs.NodeRoot, rigRoot);

        refs.Timeline_Root = GetRt(map, EpisodeNodeRigSchema.Refs.Timeline_Root, rigRoot);
        refs.TimelineBG_Image = GetImage(map, EpisodeNodeRigSchema.Refs.TimelineBG_Image, rigRoot);
        refs.TimelineEra_Text = GetText(map, EpisodeNodeRigSchema.Refs.TimelineEra_Text, rigRoot);
        refs.TimelineCursorIcon_Image = GetImage(map, EpisodeNodeRigSchema.Refs.TimelineCursorIcon_Image, rigRoot);

        refs.SelectZone_Root = GetRt(map, EpisodeNodeRigSchema.Refs.SelectZone_Root, rigRoot);
        refs.SelectZoneBG_Image = GetImage(map, EpisodeNodeRigSchema.Refs.SelectZoneBG_Image, rigRoot);

        refs.MainCard_Root = GetRt(map, EpisodeNodeRigSchema.Refs.MainCard_Root, rigRoot);
        refs.MainCardBG_Image = GetImage(map, EpisodeNodeRigSchema.Refs.MainCardBG_Image, rigRoot);
        refs.MainCardIndex_Root = GetRt(map, EpisodeNodeRigSchema.Refs.MainCardIndex_Root, rigRoot);
        refs.MainCardIndexText_Text = GetText(map, EpisodeNodeRigSchema.Refs.MainCardIndexText_Text, rigRoot);
        refs.MainCardIndexIcon_Image = GetImage(map, EpisodeNodeRigSchema.Refs.MainCardIndexIcon_Image, rigRoot);
        refs.MainCardTitle_Root = GetRt(map, EpisodeNodeRigSchema.Refs.MainCardTitle_Root, rigRoot);
        refs.MainCardTitle_Text = GetText(map, EpisodeNodeRigSchema.Refs.MainCardTitle_Text, rigRoot);
        refs.MainCardHit_Button = GetButton(map, EpisodeNodeRigSchema.Refs.MainCardHit_Button, rigRoot);

        refs.UpperLink_Root = GetRt(map, EpisodeNodeRigSchema.Refs.UpperLink_Root, rigRoot);
        refs.UpperLinkBG_Image = GetImage(map, EpisodeNodeRigSchema.Refs.UpperLinkBG_Image, rigRoot);
        refs.UpperLinkTitle_Root = GetRt(map, EpisodeNodeRigSchema.Refs.UpperLinkTitle_Root, rigRoot);
        refs.UpperLinkTitle_Text = GetText(map, EpisodeNodeRigSchema.Refs.UpperLinkTitle_Text, rigRoot);
        refs.UpperLinkHit_Button = GetButton(map, EpisodeNodeRigSchema.Refs.UpperLinkHit_Button, rigRoot);

        refs.LowerLink_Root = GetRt(map, EpisodeNodeRigSchema.Refs.LowerLink_Root, rigRoot);
        refs.LowerLinkBG_Image = GetImage(map, EpisodeNodeRigSchema.Refs.LowerLinkBG_Image, rigRoot);
        refs.LowerLinkTitle_Root = GetRt(map, EpisodeNodeRigSchema.Refs.LowerLinkTitle_Root, rigRoot);
        refs.LowerLinkTitle_Text = GetText(map, EpisodeNodeRigSchema.Refs.LowerLinkTitle_Text, rigRoot);
        refs.LowerLinkHit_Button = GetButton(map, EpisodeNodeRigSchema.Refs.LowerLinkHit_Button, rigRoot);

        refs.StateRoot_Selected = GetCanvasGroup(map, EpisodeNodeRigSchema.Refs.StateRoot_Selected, rigRoot);
        refs.StateRoot_Current = GetCanvasGroup(map, EpisodeNodeRigSchema.Refs.StateRoot_Current, rigRoot);
        refs.StateRoot_Completed = GetCanvasGroup(map, EpisodeNodeRigSchema.Refs.StateRoot_Completed, rigRoot);
        refs.StateRoot_Locked = GetCanvasGroup(map, EpisodeNodeRigSchema.Refs.StateRoot_Locked, rigRoot);

        refs.EndingBadge_Root = GetCanvasGroup(map, EpisodeNodeRigSchema.Refs.EndingBadge_Root, rigRoot);
        refs.EndingBadge_Text = GetText(map, EpisodeNodeRigSchema.Refs.EndingBadge_Text, rigRoot);

        return refs;
    }

    private RectTransform GetRt(
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map,
        EpisodeNodeRigSchema.Refs key,
        Object context)
    {
        if (!map.TryGetValue(key, out RectTransform rt) || rt == null)
        {
            Debug.LogWarning($"[EpisodeNodeRigBuilder] Missing RectTransform ref '{key}'.", context);
            return null;
        }

        return rt;
    }

    private Image GetImage(
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map,
        EpisodeNodeRigSchema.Refs key,
        Object context)
    {
        RectTransform rt = GetRt(map, key, context);

        if (rt == null)
            return null;

        Image image = rt.GetComponent<Image>();

        if (image == null)
            Debug.LogWarning($"[EpisodeNodeRigBuilder] Missing Image on '{rt.name}'.", rt);

        return image;
    }

    private Button GetButton(
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map,
        EpisodeNodeRigSchema.Refs key,
        Object context)
    {
        RectTransform rt = GetRt(map, key, context);

        if (rt == null)
            return null;

        Button button = rt.GetComponent<Button>();

        if (button == null)
            Debug.LogWarning($"[EpisodeNodeRigBuilder] Missing Button on '{rt.name}'.", rt);

        return button;
    }

    private TMP_Text GetText(
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map,
        EpisodeNodeRigSchema.Refs key,
        Object context)
    {
        RectTransform rt = GetRt(map, key, context);

        if (rt == null)
            return null;

        TMP_Text text = rt.GetComponent<TMP_Text>();

        if (text == null)
            Debug.LogWarning($"[EpisodeNodeRigBuilder] Missing TMP_Text on '{rt.name}'.", rt);

        return text;
    }

    private CanvasGroup GetCanvasGroup(
        Dictionary<EpisodeNodeRigSchema.Refs, RectTransform> map,
        EpisodeNodeRigSchema.Refs key,
        Object context)
    {
        RectTransform rt = GetRt(map, key, context);

        if (rt == null)
            return null;

        CanvasGroup group = rt.GetComponent<CanvasGroup>();

        if (group == null)
            Debug.LogWarning($"[EpisodeNodeRigBuilder] Missing CanvasGroup on '{rt.name}'.", rt);

        return group;
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

    private void PrefixAllChildren(Transform root, string prefix)
    {
        if (root == null || string.IsNullOrEmpty(prefix))
            return;

        void Walk(Transform t)
        {
            t.name = WithPrefix(prefix, t.name);

            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i));
        }

        Walk(root);
    }

    private void StretchFull(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private string WithPrefix(string prefix, string baseName)
    {
        if (string.IsNullOrEmpty(prefix))
            return baseName;

        if (baseName.StartsWith(prefix, StringComparison.Ordinal))
            return baseName;

        return $"{prefix}{baseName}";
    }
}