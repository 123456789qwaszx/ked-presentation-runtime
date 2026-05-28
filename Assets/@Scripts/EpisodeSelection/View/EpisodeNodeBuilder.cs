using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class EpisodeNodeBuilder
{
    public RectTransform BuildNodeRoot(
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
        out EpisodeNodeRefs refs)
    {
        Dictionary<EpisodeNodeSchema.Refs, RectTransform> map =
            CollectRefMap(rigRoot, nodePrefix);

        EnsureValidGraphMap(rigRoot, nodePrefix, ref map);

        refs = BuildRefs(rigRoot, map);
    }

    private void EnsureValidGraphMap(
        RectTransform rigRoot,
        string nodePrefix,
        ref Dictionary<EpisodeNodeSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(EpisodeNodeSchema.Refs)).Length;
        
        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[EpisodeNodeRigBuilder] Invalid episode node rig. Rebuilding from schema. " +
            $"rigRoot='{rigRoot.name}', prefix='{nodePrefix}', found={map.Count}, expected={expectedCount}.",
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
        foreach (EpisodeNodeSchema.NodeDef node in EpisodeNodeSchema.Nodes)
            EnsureNode(root, nodePrefix, node);
    }

    private void EnsureNode(
        RectTransform root,
        string nodePrefix,
        EpisodeNodeSchema.NodeDef node)
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
            EnsureCanvasGroup(rt, node.InitialCanvasGroupAlpha);

        if (node.NeedsImage)
            EnsureImage(rt);

        if (node.NeedsButton)
            EnsureButton(rt);

        if (node.NeedsText)
            EnsureText(rt);
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

    private void EnsureCanvasGroup(RectTransform rt, float initialAlpha)
    {
        if (rt == null)
            return;

        if (!rt.TryGetComponent(out CanvasGroup group))
            group = rt.gameObject.AddComponent<CanvasGroup>();

        group.alpha = initialAlpha;
        group.interactable = initialAlpha > 0f;
        group.blocksRaycasts = initialAlpha > 0f;
    }

    private void EnsureImage(RectTransform rt)
    {
        if (rt == null)
            return;

        if (!rt.TryGetComponent<Image>(out _))
            rt.gameObject.AddComponent<Image>();
    }

    private void EnsureButton(RectTransform rt)
    {
        if (rt == null)
            return;

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

    private void EnsureText(RectTransform rt)
    {
        if (rt == null)
            return;

        if (!rt.TryGetComponent<TMP_Text>(out _))
            rt.gameObject.AddComponent<TextMeshProUGUI>();
    }

    private Dictionary<EpisodeNodeSchema.Refs, RectTransform> CollectRefMap(
        RectTransform rigRoot,
        string nodePrefix)
    {
        Dictionary<EpisodeNodeSchema.Refs, RectTransform> map =
            new Dictionary<EpisodeNodeSchema.Refs, RectTransform>();

        foreach (EpisodeNodeSchema.Refs id in Enum.GetValues(typeof(EpisodeNodeSchema.Refs)))
        {
            string targetName = WithPrefix(nodePrefix, id.ToString());
            Transform found = FindByName(rigRoot, targetName);

            if (found != null)
                map[id] = found as RectTransform;
        }

        return map;
    }

    private EpisodeNodeRefs BuildRefs(
        RectTransform rigRoot,
        Dictionary<EpisodeNodeSchema.Refs, RectTransform> map)
    {
        EpisodeNodeRefs refs = new EpisodeNodeRefs(rigRoot);

        refs.MainCard_Root = GetRt(map, EpisodeNodeSchema.Refs.MainCard_Root, rigRoot);
        refs.MainCardBG_Image = GetImage(map, EpisodeNodeSchema.Refs.MainCardBG_Image, rigRoot);

        refs.MainCardIndex_Root = GetRt(map, EpisodeNodeSchema.Refs.MainCardIndex_Root, rigRoot);
        refs.MainCardIndexText_Text = GetText(map, EpisodeNodeSchema.Refs.MainCardIndexText_Text, rigRoot);
        refs.MainCardIndexIcon_Image = GetImage(map, EpisodeNodeSchema.Refs.MainCardIndexIcon_Image, rigRoot);

        refs.MainCardTitle_Root = GetRt(map, EpisodeNodeSchema.Refs.MainCardTitle_Root, rigRoot);
        refs.MainCardTitle_Text = GetText(map, EpisodeNodeSchema.Refs.MainCardTitle_Text, rigRoot);

        refs.MainCardHit_Button = GetButton(map, EpisodeNodeSchema.Refs.MainCardHit_Button, rigRoot);

        return refs;
    }

    private RectTransform GetRt(
        Dictionary<EpisodeNodeSchema.Refs, RectTransform> map,
        EpisodeNodeSchema.Refs key,
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
        Dictionary<EpisodeNodeSchema.Refs, RectTransform> map,
        EpisodeNodeSchema.Refs key,
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
        Dictionary<EpisodeNodeSchema.Refs, RectTransform> map,
        EpisodeNodeSchema.Refs key,
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
        Dictionary<EpisodeNodeSchema.Refs, RectTransform> map,
        EpisodeNodeSchema.Refs key,
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