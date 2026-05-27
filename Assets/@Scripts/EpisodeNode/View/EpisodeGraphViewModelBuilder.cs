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

            if (node == null || string.IsNullOrEmpty(node.Id))
                continue;

            if (runtimeState.VisibleEpisodeIds.Count > 0 &&
                !runtimeState.VisibleEpisodeIds.Contains(node.Id))
            {
                continue;
            }

            EpisodeNodeViewData nodeViewData = new EpisodeNodeViewData
            {
                EpisodeId = node.Id,
                Title = node.Title,
                IndexText = node.IndexText,
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

    private EpisodeNodeVisualState ResolveVisualState(
        string episodeId,
        EpisodeSelectionRuntimeState state)
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

    private Dictionary<string, Vector2> CalculatePositions(
        EpisodeGraphData graphData,
        EpisodeGraphLayoutOptions options)
    {
        Dictionary<string, Vector2> result =
            new Dictionary<string, Vector2>(StringComparer.Ordinal);

        int mainIndex = 0;

        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = graphData.Nodes[i];

            if (node == null)
                continue;

            if (node.Kind != EpisodeNodeKind.Main)
                continue;

            result[node.Id] = new Vector2(mainIndex * options.MainGapX, 0f);
            mainIndex++;
        }

        PositionAttachments(graphData, options, result);

        return result;
    }

    private void PositionAttachments(
        EpisodeGraphData graphData,
        EpisodeGraphLayoutOptions options,
        Dictionary<string, Vector2> result)
    {
        Dictionary<string, int> attachmentCountByParent =
            new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = graphData.Nodes[i];

            if (node == null)
                continue;

            if (node.Kind != EpisodeNodeKind.Attachment)
                continue;

            if (string.IsNullOrEmpty(node.ParentEpisodeId))
                continue;

            if (!result.TryGetValue(node.ParentEpisodeId, out Vector2 parentPos))
                continue;

            if (!attachmentCountByParent.TryGetValue(node.ParentEpisodeId, out int count))
                count = 0;

            float y = count % 2 == 0
                ? options.BranchOffsetY
                : -options.BranchOffsetY;

            float x = parentPos.x + options.MainGapX * 0.45f;

            result[node.Id] = new Vector2(x, y);

            attachmentCountByParent[node.ParentEpisodeId] = count + 1;
        }
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