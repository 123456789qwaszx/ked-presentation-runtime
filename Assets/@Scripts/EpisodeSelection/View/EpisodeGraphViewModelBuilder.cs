using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EpisodeGraphViewModelBuilder
{
    private readonly EpisodeSelectionStateData _episodeSelectionStateData;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;

    public EpisodeGraphViewModelBuilder(
        EpisodeSelectionStateData stateData,
        EpisodeGraphLayoutOptions layoutOptions)
    {
        _episodeSelectionStateData = stateData;
        _layoutOptions = layoutOptions;
    }

    public EpisodeGraphViewData Build(EpisodeGraphData graphData)
    {
        EpisodeGraphViewData viewData = new EpisodeGraphViewData();

        if (graphData == null)
            return viewData;

        Dictionary<string, Vector2> positions = CalculatePositions(graphData, _layoutOptions);

        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = graphData.Nodes[i];

            if (node == null)
                continue;

            if (!_episodeSelectionStateData.VisibleEpisodeIds.Contains(node.Id))
                continue;

            EpisodeNodeViewData nodeViewData = new EpisodeNodeViewData
            {
                EpisodeId = node.Id,
                Title = node.Title,
                IndexText = node.IndexText,
                AnchoredPosition = positions.TryGetValue(node.Id, out Vector2 pos)
                    ? pos
                    : Vector2.zero,
                Size = _layoutOptions.NodeSize,
                VisualState = ResolveVisualState(node.Id),
            };

            viewData.Nodes.Add(nodeViewData);
        }

        viewData.ContentSize = CalculateContentSize(
            viewData.Nodes,
            _layoutOptions);

        return viewData;
    }

    private EpisodeNodeVisualState ResolveVisualState(string episodeId)
    {
        if (_episodeSelectionStateData == null)
            return EpisodeNodeVisualState.Normal;

        if (_episodeSelectionStateData.LockedEpisodeIds.Contains(episodeId))
            return EpisodeNodeVisualState.Locked;

        if (string.Equals(
                _episodeSelectionStateData.SelectedEpisodeId,
                episodeId,
                StringComparison.Ordinal))
        {
            return EpisodeNodeVisualState.Selected;
        }

        if (string.Equals(
                _episodeSelectionStateData.CurrentEpisodeId,
                episodeId,
                StringComparison.Ordinal))
        {
            return EpisodeNodeVisualState.Current;
        }

        if (_episodeSelectionStateData.ClearedEpisodeIds.Contains(episodeId))
            return EpisodeNodeVisualState.Completed;

        return EpisodeNodeVisualState.Normal;
    }

    private Dictionary<string, Vector2> CalculatePositions(
        EpisodeGraphData graphData, 
        EpisodeGraphLayoutOptions layoutOptions)
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

            result[node.Id] = new Vector2(
                mainIndex * layoutOptions.MainGapX,
                0f);

            mainIndex++;
        }

        PositionAttachments(graphData, layoutOptions, result);

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

            int count = 0;

            if (attachmentCountByParent.TryGetValue(
                    node.ParentEpisodeId,
                    out int existingCount))
            {
                count = existingCount;
            }

            float y = count % 2 == 0
                ? options.BranchOffsetY
                : -options.BranchOffsetY;

            float x = parentPos.x;

            result[node.Id] = new Vector2(x, y);
            attachmentCountByParent[node.ParentEpisodeId] = count + 1;
        }
    }

    private Vector2 CalculateContentSize(
        List<EpisodeNodeViewData> nodes,
        EpisodeGraphLayoutOptions options)
    {
        return new Vector2(
            nodes.Count * options.MainGapX + options.Padding.x * 2f,
            options.BranchOffsetY * 2f + options.Padding.y * 2f);
    }
}