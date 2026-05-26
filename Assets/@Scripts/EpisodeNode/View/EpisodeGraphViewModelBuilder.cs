using System;
using System.Collections.Generic;
using UnityEngine;

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

            if (string.IsNullOrEmpty(node.Id))
                continue;

            EpisodeNodeViewData nodeViewData = new EpisodeNodeViewData
            {
                EpisodeId = node.Id,
                AnchoredPosition = positions.TryGetValue(node.Id, out Vector2 pos)
                    ? pos
                    : Vector2.zero,
                Size = options.NodeSize,
                VisualState = ResolveVisualState(node.Id, runtimeState),
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

    private Dictionary<string, Vector2> CalculatePositions(EpisodeGraphData graphData, EpisodeGraphLayoutOptions options)
    {
        Dictionary<string, Vector2> result = new Dictionary<string, Vector2>(StringComparer.Ordinal);

        int mainIndex = 0;

        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = graphData.Nodes[i];

            if (node.Kind != EpisodeNodeKind.Main)
                continue;

            result[node.Id] = new Vector2(mainIndex * options.MainGapX, 0f);

            mainIndex++;
        }

        return result;
    }

    private Vector2 CalculateContentSize(
        List<EpisodeNodeViewData> nodes,
        EpisodeGraphLayoutOptions options)
    {
        return new Vector2(
            Mathf.Max(800f, nodes.Count * options.MainGapX + options.Padding.x * 2f),
            Mathf.Max(400f, options.BranchOffsetY * 2f + options.Padding.y * 2f));
    }
}