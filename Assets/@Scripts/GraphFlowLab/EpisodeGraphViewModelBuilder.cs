using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EpisodeGraphLayoutOptions
{
    [Header("Node")]
    public Vector2 NodeSize = new Vector2(360f, 160f);

    [Header("Main Path")]
    public float MainGapX = 460f;

    [Header("Branch")]
    public float BranchOffsetY = 220f;

    [Header("Content Padding")]
    public Vector2 Padding = new Vector2(240f, 260f);

    public static EpisodeGraphLayoutOptions Default()
    {
        return new EpisodeGraphLayoutOptions();
    }

    public static EpisodeGraphLayoutOptions Compact()
    {
        return new EpisodeGraphLayoutOptions
        {
            NodeSize = new Vector2(300f, 140f),
            MainGapX = 380f,
            BranchOffsetY = 180f,
            Padding = new Vector2(180f, 220f)
        };
    }

    public static EpisodeGraphLayoutOptions Wide()
    {
        return new EpisodeGraphLayoutOptions
        {
            NodeSize = new Vector2(360f, 160f),
            MainGapX = 520f,
            BranchOffsetY = 240f,
            Padding = new Vector2(280f, 300f)
        };
    }
}

public sealed class EpisodeGraphViewModelBuilder
{
    public EpisodeGraphViewData Build(
        EpisodeGraphData graphData,
        EpisodeSelectionRuntimeState runtimeState,
        EpisodeGraphLayoutOptions options)
    {
        EpisodeGraphViewData viewData = new();

        if (graphData == null)
            return viewData;

        Dictionary<string, Vector2> positions = CalculatePositions(graphData, options);

        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = graphData.Nodes[i];

            if (string.IsNullOrEmpty(node.EpisodeId))
                continue;

            EpisodeNodeViewData nodeViewData = new EpisodeNodeViewData
            {
                EpisodeId = node.EpisodeId,
                AnchoredPosition = positions.TryGetValue(node.EpisodeId, out Vector2 pos)
                    ? pos
                    : Vector2.zero,
                Size = options.NodeSize,
                VisualState = ResolveVisualState(node.EpisodeId, runtimeState),
                UpperLink = BuildLinkViewData(graphData, node.EpisodeId, EpisodeNodeLinkSlot.Upper),
                LowerLink = BuildLinkViewData(graphData, node.EpisodeId, EpisodeNodeLinkSlot.Lower)
            };

            viewData.Nodes.Add(nodeViewData);
        }

        viewData.ContentSize = CalculateContentSize(viewData.Nodes, options);
        return viewData;
    }

    private EpisodeNodeVisualState ResolveVisualState(string episodeId, EpisodeSelectionRuntimeState state)
    {
        if (state.LockedEpisodeIds.Contains(episodeId))
            return EpisodeNodeVisualState.Locked;

        if (state.SelectedEpisodeId == episodeId)
            return EpisodeNodeVisualState.Selected;

        if (state.CurrentEpisodeId == episodeId)
            return EpisodeNodeVisualState.Current;

        if (state.ClearedEpisodeIds.Contains(episodeId))
            return EpisodeNodeVisualState.Completed;

        return EpisodeNodeVisualState.Normal;
    }

    private EpisodeNodeLinkViewData BuildLinkViewData(
        EpisodeGraphData graphData,
        string ownerEpisodeId,
        EpisodeNodeLinkSlot slot)
    {
        // graphData에서 ownerEpisodeId의 upper/lower attachment를 찾아 ViewData로 변환
        return null;
    }

    private Dictionary<string, Vector2> CalculatePositions(EpisodeGraphData graphData, EpisodeGraphLayoutOptions options)
    {
        Dictionary<string, Vector2> result = new Dictionary<string, Vector2>(StringComparer.Ordinal);

        int mainIndex = 0;

        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = graphData.Nodes[i];

            if (node.Kind != EpisodeNodeKind.Main)
                continue;

            result[node.EpisodeId] = new Vector2(
                mainIndex * options.MainGapX,
                0f);

            mainIndex++;
        }

        return result;
    }

    private Vector2 CalculateContentSize(
        List<EpisodeNodeViewData> nodes,
        EpisodeGraphLayoutOptions options)
    {
        // 일단 단순 계산
        return new Vector2(
            Mathf.Max(800f, nodes.Count * options.MainGapX + options.Padding.x * 2f),
            Mathf.Max(400f, options.BranchOffsetY * 2f + options.Padding.y * 2f));
    }
}