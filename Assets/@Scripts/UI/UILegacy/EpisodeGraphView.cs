using System;
using System.Collections.Generic;
using UnityEngine;


public sealed class EpisodeGraphView : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private EpisodeNodeView nodePrefab;
    [SerializeField] private HorizontalScrollContentFitter sizer;
    

    // episodeId -> view (노드의 정체성을 고정)
    private readonly Dictionary<string, EpisodeNodeView> _byId = new(StringComparer.Ordinal);

    // 풀(재사용 가능한 비활성 뷰) - ClearAll 이후 챕터 전환 등에서만 재사용
    private readonly List<EpisodeNodeView> _pool = new();
    
    private Action<string> _onMain;
    private Action<string, LinkKind, string> _onBranch;

    public void SetHandlers(
        Action<string> onMainClicked,
        Action<string, LinkKind, string> onBranchClicked)
    {
        _onMain = onMainClicked;
        _onBranch = onBranchClicked;
    }

    public void Render(in EpisodeGraphModel graph)
    {
        if (content == null || nodePrefab == null) return;

        // 이번 렌더에서 쓰인 id 체크(끝나고 남는 애들은 숨김)
        var used = new HashSet<string>(StringComparer.Ordinal);

        // 노드들 표시/생성
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            EpisodeNodeModel model = graph.Nodes[i];
            if (string.IsNullOrEmpty(model.EpisodeId))
                continue;

            string id = model.EpisodeId;
            used.Add(id);

            EpisodeNodeView view = GetOrCreateView(id);

            if (!view.gameObject.activeSelf)
                view.gameObject.SetActive(true);

            ((RectTransform)view.transform).anchoredPosition = model.AnchoredPos;
            view.Present(model);
        }

        // 이번에 사용되지 않은 노드들은 숨김(정체성 고정이 목적이므로 _byId에 남겨둠)
        foreach (var kv in _byId)
        {
            EpisodeNodeView v = kv.Value;
            if (!used.Contains(kv.Key) && v.gameObject.activeSelf)
                v.gameObject.SetActive(false);
        }
        
        if (sizer != null)
            sizer.RebuildSize();
    }

    private EpisodeNodeView GetOrCreateView(string episodeId)
    {
        // 이미 있으면 그대로
        if (_byId.TryGetValue(episodeId, out var existing) && existing != null)
        {
            return existing;
        }

        // 풀에서 하나 꺼내기 (가능하면 재사용)
        for (int i = 0; i < _pool.Count; i++)
        {
            var candidate = _pool[i];
            if (candidate != null && !candidate.gameObject.activeSelf)
            {
                _pool.RemoveAt(i);

                // Bind once per instance: candidate가 풀에 들어갈 때 이미 바인딩 되어 있어야 함
                // (Instantiate 시에만 바인딩하고, ClearAll은 바인딩을 해제하지 않음)
                _byId[episodeId] = candidate;
                return candidate;
            }
        }

        // 없으면 새로 생성
        EpisodeNodeView view = Instantiate(nodePrefab!, content);
        view.gameObject.SetActive(false); // 깜빡임 방지
        
        // view 패치

        // Bind once per instance
        view.OnMainCardClicked += HandleMain;
        view.OnBranchNodeClicked += HandleBranch;

        _byId[episodeId] = view;
        return view;
    }

    // 완전히 폐기하고 싶을 때 (예: 챕터 변경시)
    public void ClearAll()
    {
        // 현재 보유한 뷰들 전부 비활성화
        foreach (var kv in _byId)
        {
            if (kv.Value != null)
                kv.Value.gameObject.SetActive(false);
        }

        // 풀로 이동해서 다음 챕터에서 재활용 가능하게
        _pool.Clear();
        foreach (var kv in _byId)
        {
            if (kv.Value != null)
                _pool.Add(kv.Value);
        }

        _byId.Clear();
    }

    #region Event Handlers

    private void HandleMain(string ownerId)
    {
        _onMain(ownerId);
    }

    private void HandleBranch(string ownerId, LinkKind kind, string targetId)
    {
        _onBranch(ownerId, kind, targetId);
    }

    #endregion
}