using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IEpisodeGraphScrollRootProvider
{
    ScrollRect GraphScrollRect { get; }
    RectTransform GraphContent { get; }
    RectTransform GraphViewport { get; }
}

public sealed partial class EpisodeSelectionPanel : IEpisodeGraphScrollRootProvider
{
    public ScrollRect GraphScrollRect => View?.Rect(Refs.ButtonViewport)?.GetComponent<ScrollRect>();
    public RectTransform GraphContent => View?.Rect(Refs.EpisodeButtons);
    public RectTransform GraphViewport => View?.Rect(Refs.ButtonViewport);
}

public sealed class EpisodeGraphRenderer
{
    private IEpisodeGraphScrollRootProvider _rootProvider;
    private IEpisodeGraphScrollRootProvider RootProvider => _rootProvider ??= ResolveRootProvider();
    private IEpisodeGraphScrollRootProvider ResolveRootProvider() => UIManager.Instance.GetUI<EpisodeSelectionPanel>();
    
    
    private readonly RectTransform _content;
    private readonly RectTransform _nodeRigPrefab;

    private readonly EpisodeNodeBuilder _builder = new();

    private readonly Dictionary<string, RuntimeNode> _activeById = new(StringComparer.Ordinal);

    private readonly List<RuntimeNode> _pool = new();

    private Action<string> _onMainClicked;

    public EpisodeGraphRenderer(RectTransform nodeRigPrefab)
    {
        _nodeRigPrefab = nodeRigPrefab;
        _content = RootProvider.GraphContent;
    }
    
    public void Render(EpisodeGraphViewData viewData)
    {
        HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);

        RenderNodes(viewData, used);
        DeactivateUnused(used);
        _content.sizeDelta = viewData.ContentSize;
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
    

    private void RenderNodes(EpisodeGraphViewData viewData, HashSet<string> used)
    {
        for (int i = 0; i < viewData.Nodes.Count; i++)
        {
            EpisodeNodeViewData nodeViewData = viewData.Nodes[i];
            RenderNode(nodeViewData, used);
        }
    }

    private void RenderNode(EpisodeNodeViewData nodeViewData, HashSet<string> used)
    {
        if (string.IsNullOrEmpty(nodeViewData.EpisodeId))
            return;

        used.Add(nodeViewData.EpisodeId);

        RuntimeNode node = GetOrCreateNode(nodeViewData.EpisodeId);
        if (node == null || node.Root == null || node.View == null) return;

        node.Root.anchoredPosition = nodeViewData.AnchoredPosition;
        node.Root.sizeDelta = nodeViewData.Size;
        node.Root.gameObject.SetActive(true);
        
        node.View.Present(nodeViewData);
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
        string prefix = BuildNodePrefix(episodeId);

        RectTransform root = _builder.BuildNodeRoot(_nodeRigPrefab, prefix);

        root.SetParent(_content, false);
        root.gameObject.SetActive(false);

        _builder.BindRefsFromRoot(root, prefix, out EpisodeNodeRefs refs);

        EpisodeNodeView view = new EpisodeNodeView(refs);

        view.MainClicked += HandleMainClicked;

        return new RuntimeNode(
            episodeId,
            prefix,
            root,
            view,
            HandleMainClicked);
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

            if (removeKeys == null)
                removeKeys = new List<string>();

            removeKeys.Add(kv.Key);
        }

        if (removeKeys == null)
            return;

        for (int i = 0; i < removeKeys.Count; i++)
            _activeById.Remove(removeKeys[i]);
    }
    
    #region Handlers
    public void SetHandlers(Action<string> onMainClicked)
    {
        _onMainClicked = onMainClicked;
    }

    private void HandleMainClicked(string episodeId)
    {
        _onMainClicked?.Invoke(episodeId);
    }
    #endregion

    #region Helpers
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
    #endregion

    private sealed class RuntimeNode : IDisposable
    {
        private readonly Action<string> _mainClickedHandler;

        public string EpisodeId { get; private set; }
        public string Prefix { get; private set; }

        public RectTransform Root { get; }
        public EpisodeNodeView View { get; }

        public RuntimeNode(
            string episodeId,
            string prefix,
            RectTransform root,
            EpisodeNodeView view,
            Action<string> mainClickedHandler)
        {
            EpisodeId = episodeId ?? "";
            Prefix = prefix ?? "";

            Root = root;
            View = view;
            _mainClickedHandler = mainClickedHandler;
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
            {
                View.MainClicked -= _mainClickedHandler;
                View.Dispose();
            }

            if (Root != null)
            {
                Root.SetParent(null, false);
                UnityEngine.Object.Destroy(Root.gameObject);
            }
        }
    }
}