using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EpisodeGraphView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform content;

    [Header("Optional Prefab")]
    [SerializeField] private RectTransform nodeRigPrefab;

    [Header("Sizing")]
    [SerializeField] private HorizontalScrollContentFitter sizer;

    private readonly EpisodeNodeRigBuilder _builder = new();

    private readonly Dictionary<string, RuntimeNode> _byId = new(StringComparer.Ordinal);
    private readonly List<RuntimeNode> _pool = new();

    private Action<string> _onMainClicked;
    private Action<string, LinkKind, string> _onBranchClicked;

    private void OnDestroy()
    {
        DisposeAll();
    }

    public void SetHandlers(
        Action<string> onMainClicked,
        Action<string, LinkKind, string> onBranchClicked)
    {
        _onMainClicked = onMainClicked;
        _onBranchClicked = onBranchClicked;
    }

    public void Render(in EpisodeGraphModel graph)
    {
        if (content == null)
            return;

        if (graph.Nodes == null)
            return;

        HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            EpisodeNodeModel model = graph.Nodes[i];

            if (string.IsNullOrEmpty(model.EpisodeId))
                continue;

            used.Add(model.EpisodeId);

            RuntimeNode node = GetOrCreateNode(model.EpisodeId);

            if (node.Root == null)
                continue;

            if (!node.Root.gameObject.activeSelf)
                node.Root.gameObject.SetActive(true);

            node.Root.anchoredPosition = model.AnchoredPos;
            node.View.Present(model);
        }

        foreach (KeyValuePair<string, RuntimeNode> kv in _byId)
        {
            RuntimeNode node = kv.Value;

            if (node == null || node.Root == null)
                continue;

            if (!used.Contains(kv.Key) && node.Root.gameObject.activeSelf)
                node.Root.gameObject.SetActive(false);
        }

        if (sizer != null)
            sizer.RebuildSize();
    }

    public void ClearAll()
    {
        foreach (KeyValuePair<string, RuntimeNode> kv in _byId)
        {
            RuntimeNode node = kv.Value;

            if (node == null)
                continue;

            if (node.Root != null)
                node.Root.gameObject.SetActive(false);

            if (!_pool.Contains(node))
                _pool.Add(node);
        }

        _byId.Clear();
    }

    public void DisposeAll()
    {
        foreach (KeyValuePair<string, RuntimeNode> kv in _byId)
        {
            if (kv.Value != null)
                kv.Value.Dispose();
        }

        for (int i = 0; i < _pool.Count; i++)
        {
            RuntimeNode node = _pool[i];

            if (node != null)
                node.Dispose();
        }

        _byId.Clear();
        _pool.Clear();
    }

    private RuntimeNode GetOrCreateNode(string episodeId)
    {
        if (_byId.TryGetValue(episodeId, out RuntimeNode existing) && existing != null)
            return existing;

        RuntimeNode pooled = TakeFromPool();

        if (pooled != null)
        {
            if (pooled.Root != null)
                pooled.Root.name = BuildNodePrefix(episodeId) + "EpisodeNodeRig";

            _byId[episodeId] = pooled;
            return pooled;
        }

        RuntimeNode created = CreateNode(episodeId);
        _byId[episodeId] = created;
        return created;
    }

    private RuntimeNode TakeFromPool()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            RuntimeNode candidate = _pool[i];

            _pool.RemoveAt(i);

            if (candidate != null && candidate.Root != null)
                return candidate;

            if (candidate != null)
                candidate.Dispose();

            i--;
        }

        return null;
    }

    private RuntimeNode CreateNode(string episodeId)
    {
        string prefix = BuildNodePrefix(episodeId);

        RectTransform root = _builder.BuildNodeRigRoot(
            nodeRigPrefab,
            prefix,
            "EpisodeNodeRig"
        );

        root.SetParent(content, false);
        root.gameObject.SetActive(false);

        _builder.BindRefsFromRoot(root, prefix, out EpisodeNodeRigRefs refs);

        EpisodeNodeView view = new EpisodeNodeView(refs);
        view.MainClicked += HandleMainClicked;
        view.BranchClicked += HandleBranchClicked;

        return new RuntimeNode(root, view);
    }

    private void HandleMainClicked(string episodeId)
    {
        _onMainClicked?.Invoke(episodeId);
    }

    private void HandleBranchClicked(
        string ownerId,
        LinkKind kind,
        string targetId)
    {
        _onBranchClicked?.Invoke(ownerId, kind, targetId);
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
                (c >= '0' && c <= '9') ||
                c == '_';

            if (!valid)
                chars[i] = '_';
        }

        return new string(chars);
    }

    private sealed class RuntimeNode
    {
        public readonly RectTransform Root;
        public readonly EpisodeNodeView View;

        public RuntimeNode(
            RectTransform root,
            EpisodeNodeView view)
        {
            Root = root;
            View = view;
        }

        public void Dispose()
        {
            if (View != null)
                View.Dispose();

            if (Root != null)
                Object.Destroy(Root.gameObject);
        }
    }
}