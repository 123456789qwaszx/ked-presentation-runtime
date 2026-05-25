using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EpisodeGraphView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform content;

    [Header("Optional Prefab")]
    [SerializeField] private RectTransform nodeRigPrefab;

    [Header("Sizing")]
    [SerializeField] private HorizontalScrollContentFitter sizer;

    [Header("Options")]
    [SerializeField] private string rigRootName = "EpisodeNodeRig";

    private readonly EpisodeNodeRigBuilder _builder = new();

    private readonly Dictionary<string, RuntimeNode> _activeById = new(StringComparer.Ordinal);
    private readonly List<RuntimeNode> _pool = new();

    private Action<string> _onMainClicked;
    private Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkModel> _onLinkClicked;

    private void OnDestroy()
    {
        DisposeAll();
    }

    public void SetHandlers(
        Action<string> onMainClicked,
        Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkModel> onLinkClicked)
    {
        _onMainClicked = onMainClicked;
        _onLinkClicked = onLinkClicked;
    }

    public void Render(in EpisodeGraphModel graph)
    {
        
        if (content == null)
        {
            Debug.LogWarning("[EpisodeGraphView] Content is null.", this);
            return;
        }

        int count = graph.Nodes != null ? graph.Nodes.Count : 0;
        Debug.Log($"[EpisodeGraphView] Render nodes={count}", this);

        HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);

        if (graph.Nodes != null)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                EpisodeNodeModel model = graph.Nodes[i];

                if (string.IsNullOrEmpty(model.EpisodeId))
                    continue;

                used.Add(model.EpisodeId);

                RuntimeNode node = GetOrCreateNode(model.EpisodeId);

                if (node == null || node.Root == null || node.View == null)
                    continue;

                if (!node.Root.gameObject.activeSelf)
                    node.Root.gameObject.SetActive(true);

                node.View.Present(model);
            }
        }

        DeactivateUnused(used);

        if (sizer != null)
            sizer.RebuildSize();
    }

    public void ClearAll()
    {
        foreach (KeyValuePair<string, RuntimeNode> kv in _activeById)
        {
            RuntimeNode node = kv.Value;

            if (node == null)
                continue;

            if (node.Root != null)
                node.Root.gameObject.SetActive(false);

            if (!_pool.Contains(node))
                _pool.Add(node);
        }

        _activeById.Clear();

        if (sizer != null)
            sizer.RebuildSize();
    }

    public void DisposeAll()
    {
        foreach (KeyValuePair<string, RuntimeNode> kv in _activeById)
        {
            RuntimeNode node = kv.Value;

            if (node != null)
                node.Dispose();
        }

        for (int i = 0; i < _pool.Count; i++)
        {
            RuntimeNode node = _pool[i];

            if (node != null)
                node.Dispose();
        }

        _activeById.Clear();
        _pool.Clear();
    }

    private RuntimeNode GetOrCreateNode(string episodeId)
    {
        if (_activeById.TryGetValue(episodeId, out RuntimeNode existing) && existing != null)
            return existing;

        RuntimeNode pooled = TakeFromPool();

        if (pooled != null)
        {
            pooled.RebindEpisodeId(episodeId);
            _activeById[episodeId] = pooled;
            return pooled;
        }

        RuntimeNode created = CreateNode(episodeId);
        _activeById[episodeId] = created;
        return created;
    }

    private RuntimeNode TakeFromPool()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            RuntimeNode candidate = _pool[i];
            _pool.RemoveAt(i);

            if (candidate != null && candidate.Root != null && candidate.View != null)
                return candidate;

            if (candidate != null)
                candidate.Dispose();

            i--;
        }

        return null;
    }

    private RuntimeNode CreateNode(string episodeId)
    {
        Debug.Log($"[EpisodeGraphView] CreateNode episodeId='{episodeId}'", this);
        string prefix = BuildNodePrefix(episodeId);

        RectTransform root = _builder.BuildNodeRigRoot(
            nodeRigPrefab,
            prefix,
            rigRootName);

        if (root == null)
            return null;

        root.SetParent(content, false);
        root.gameObject.SetActive(false);

        _builder.BindRefsFromRoot(root, prefix, out EpisodeNodeRigRefs refs);

        EpisodeNodeView view = new EpisodeNodeView(refs);

        view.MainClicked += HandleMainClicked;
        view.LinkClicked += HandleLinkClicked;

        return new RuntimeNode(
            episodeId,
            prefix,
            root,
            view);
    }

    private void DeactivateUnused(HashSet<string> used)
    {
        List<string> removeKeys = null;

        foreach (KeyValuePair<string, RuntimeNode> kv in _activeById)
        {
            if (used.Contains(kv.Key))
                continue;

            RuntimeNode node = kv.Value;

            if (node != null && node.Root != null)
                node.Root.gameObject.SetActive(false);

            if (node != null && !_pool.Contains(node))
                _pool.Add(node);

            removeKeys ??= new List<string>();
            removeKeys.Add(kv.Key);
        }

        if (removeKeys == null)
            return;

        for (int i = 0; i < removeKeys.Count; i++)
            _activeById.Remove(removeKeys[i]);
    }

    private void HandleMainClicked(string episodeId)
    {
        _onMainClicked?.Invoke(episodeId);
    }

    private void HandleLinkClicked(
        string ownerEpisodeId,
        EpisodeNodeLinkSlot slot,
        EpisodeNodeLinkModel link)
    {
        _onLinkClicked?.Invoke(ownerEpisodeId, slot, link);
    }

    private static string BuildNodePrefix(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return "node_";

        return SanitizePrefix(episodeId) + "_";
    }

    private static string SanitizePrefix(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "node";

        char[] chars = value.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];

            bool valid =
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9');

            if (!valid)
                chars[i] = '_';
        }

        return new string(chars);
    }

    private sealed class RuntimeNode : IDisposable
    {
        public string EpisodeId { get; private set; }
        public string Prefix { get; private set; }

        public RectTransform Root { get; }
        public EpisodeNodeView View { get; }

        public RuntimeNode(
            string episodeId,
            string prefix,
            RectTransform root,
            EpisodeNodeView view)
        {
            EpisodeId = episodeId ?? "";
            Prefix = prefix ?? "";

            Root = root;
            View = view;
        }

        public void RebindEpisodeId(string episodeId)
        {
            EpisodeId = episodeId ?? "";
            Prefix = BuildNodePrefix(EpisodeId);

            if (Root != null)
                Root.name = Prefix + "EpisodeNodeRig";
        }

        public void Dispose()
        {
            if (View != null)
                View.Dispose();

            if (Root != null)
            {
                Root.SetParent(null, false);
                Destroy(Root.gameObject);
            }
        }
    }
}