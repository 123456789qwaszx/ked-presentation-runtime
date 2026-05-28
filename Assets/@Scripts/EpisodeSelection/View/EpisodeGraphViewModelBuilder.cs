using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EpisodeGraphViewModelBuilder
{
    private readonly EpisodeGraphData _runtimeModel;
    private readonly EpisodeSelectionStateData _episodeSelectionStateData;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;

    public EpisodeGraphViewModelBuilder(
        EpisodeGraphData runtimeModel, 
        EpisodeSelectionStateData stateData, 
        EpisodeGraphLayoutOptions layoutOptions)
    {
        _runtimeModel = runtimeModel;
        _episodeSelectionStateData = stateData;
        _layoutOptions = layoutOptions;
    }

    public EpisodeGraphViewData Build()
    {
        EpisodeGraphViewData viewData = new();
        
        EpisodeGraphLayoutOptions options = _layoutOptions;
        Dictionary<string, Vector2> positions = CalculatePositions(_runtimeModel, options);

        for (int i = 0; i < _runtimeModel.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = _runtimeModel.Nodes[i];

            // if (!_runtimeModel.ShouldShowEpisode(node.Id))
            //     continue;

            EpisodeNodeViewData nodeViewData = new EpisodeNodeViewData
            {
                EpisodeId = node.Id,
                Title = node.Title,
                IndexText = node.IndexText,
                AnchoredPosition = positions.TryGetValue(node.Id, out Vector2 pos)
                    ? pos
                    : Vector2.zero,
                Size = options.NodeSize,
                VisualState = ResolveVisualState(node.Id),
            };

            viewData.Nodes.Add(nodeViewData);
        }

        viewData.ContentSize = CalculateContentSize(viewData.Nodes, options);
        return viewData;
    }
    
    private EpisodeNodeVisualState ResolveVisualState(string episodeId)
    {
        if (_episodeSelectionStateData.LockedEpisodeIds.Contains(episodeId))
            return EpisodeNodeVisualState.Locked;

        if (string.Equals(_episodeSelectionStateData.SelectedEpisodeId, episodeId, StringComparison.Ordinal))
            return EpisodeNodeVisualState.Selected;

        if (string.Equals(_episodeSelectionStateData.CurrentEpisodeId, episodeId, StringComparison.Ordinal))
            return EpisodeNodeVisualState.Current;

        if (_episodeSelectionStateData.ClearedEpisodeIds.Contains(episodeId))
            return EpisodeNodeVisualState.Completed;

        return EpisodeNodeVisualState.Normal;
    }

    private Dictionary<string, Vector2> CalculatePositions(
        EpisodeGraphData graphData,
        EpisodeGraphLayoutOptions options)
    {
        Dictionary<string, Vector2> result = new(StringComparer.Ordinal);

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
        Dictionary<string, int> attachmentCountByParent = new(StringComparer.Ordinal);

        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = graphData.Nodes[i];

            if (node == null) continue;
            if (node.Kind != EpisodeNodeKind.Attachment) continue;
            if (string.IsNullOrEmpty(node.ParentEpisodeId)) continue;

            int count = attachmentCountByParent.GetValueOrDefault(node.ParentEpisodeId);
            Vector2 parentPos = result[node.ParentEpisodeId];

            float y = count % 2 == 0
                ? options.BranchOffsetY
                : -options.BranchOffsetY;
            
            float x = parentPos.x;
            //float x = parentPos.x + options.MainGapX * 0.45f;

            result[node.Id] = new Vector2(x, y);

            attachmentCountByParent[node.ParentEpisodeId] = count + 1;
        }
    }
    
    private Vector2 CalculateContentSize(List<EpisodeNodeViewData> nodes, EpisodeGraphLayoutOptions options)
    {
        return new Vector2(
            nodes.Count * options.MainGapX + options.Padding.x * 2f,
            options.BranchOffsetY * 2f + options.Padding.y * 2f);
    }
}