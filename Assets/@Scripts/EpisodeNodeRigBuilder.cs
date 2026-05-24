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
                typeof(RectTransform)
            );

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
            $"[EpisodeNodeRigBuilder] Invalid episode node rig. " +
            $"Rebuilding from EpisodeNodeRigSchema. " +
            $"rigRoot='{rigRoot.name}', prefix='{nodePrefix}'.",
            rigRoot
        );

        for (int i = rigRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rigRoot.GetChild(i);

            // Destroy() is delayed until the end of the frame.
            // Detach first so EnsureGraph() cannot find soon-to-be-destroyed nodes.
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

        RectTransform rt = EnsureRect(parent, WithPrefix(nodePrefix, node.Id.ToString()));

        if (node.NeedsBottomPivot)
            rt.pivot = new Vector2(0.5f, 0f);

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
            string nodeName = WithPrefix(nodePrefix, id.ToString());
            RectTransform target = FindByName(rigRoot, nodeName) as RectTransform;

            if (target != null)
                map[id] = target;
        }

        return map;
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

        refs.UpperAttachment_Root = GetRt(map, EpisodeNodeRigSchema.Refs.UpperAttachment_Root, rigRoot);
        refs.UpperAttachmentBG_Image = GetImage(map, EpisodeNodeRigSchema.Refs.UpperAttachmentBG_Image, rigRoot);
        refs.UpperAttachmentTitle_Root = GetRt(map, EpisodeNodeRigSchema.Refs.UpperAttachmentTitle_Root, rigRoot);
        refs.UpperAttachmentTitle_Text = GetText(map, EpisodeNodeRigSchema.Refs.UpperAttachmentTitle_Text, rigRoot);
        refs.UpperAttachmentHit_Button = GetButton(map, EpisodeNodeRigSchema.Refs.UpperAttachmentHit_Button, rigRoot);

        refs.LowerAttachment_Root = GetRt(map, EpisodeNodeRigSchema.Refs.LowerAttachment_Root, rigRoot);
        refs.LowerAttachmentBG_Image = GetImage(map, EpisodeNodeRigSchema.Refs.LowerAttachmentBG_Image, rigRoot);
        refs.LowerAttachmentTitle_Root = GetRt(map, EpisodeNodeRigSchema.Refs.LowerAttachmentTitle_Root, rigRoot);
        refs.LowerAttachmentTitle_Text = GetText(map, EpisodeNodeRigSchema.Refs.LowerAttachmentTitle_Text, rigRoot);
        refs.LowerAttachmentHit_Button = GetButton(map, EpisodeNodeRigSchema.Refs.LowerAttachmentHit_Button, rigRoot);

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