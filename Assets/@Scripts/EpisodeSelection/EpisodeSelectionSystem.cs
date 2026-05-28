using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EpisodeSelectionSystem
{
    private readonly Dictionary<string, Dictionary<string, EpisodeYarnEntryData>> _yarnEntriesByChapterId;
    private readonly Dictionary<string, EpisodeGraphData> _graphDataByChapterId;
    private readonly Dictionary<string, EpisodeProgressionRuleData> _ruleDataByChapterId;

    private readonly EpisodeGraphRenderer _episodeGraphRenderer;

    private readonly EpisodeConditionEvaluator _conditionEvaluator;
    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;

    public EpisodeSelectionSystem(
        ChapterEpisodeProgressionCatalogSO progressionCatalog,
        EpisodeGraphLayoutOptions layoutOptions,
        EpisodeYarnEntryMapBuilder yarnEntryMapBuilder,
        EpisodeProgressionGraphDataBuilder graphDataBuilder,
        EpisodeProgressionRuleDataBuilder ruleDataBuilder,
        EpisodeSelectionStateData selectionState,
        EpisodeGraphRenderer episodeGraphRenderer)
    {
        SelectionState = selectionState;
        _episodeGraphRenderer = episodeGraphRenderer;

        _yarnEntriesByChapterId = yarnEntryMapBuilder.Build(progressionCatalog);
        _graphDataByChapterId = graphDataBuilder.Build(progressionCatalog);
        _ruleDataByChapterId = ruleDataBuilder.Build(progressionCatalog);

        _conditionEvaluator = new EpisodeConditionEvaluator(SelectionState);
        _viewModelBuilder = new EpisodeGraphViewModelBuilder(SelectionState, layoutOptions);
    }

    private Dictionary<string, EpisodeYarnEntryData> _yarnEntryByEpisodeId;
    private EpisodeGraphData _currentGraphData;
    private EpisodeProgressionRuleData _progressionRules;
    
    public EpisodeSelectionStateData SelectionState { get; }
    
    public string GetYarnNodeName(string episodeId)
    {
        const string fallback = "yarnNodeFallback";

        if (!_yarnEntryByEpisodeId.TryGetValue(episodeId, out EpisodeYarnEntryData entry))
            return fallback;

        return entry.YarnNodeName;
    }

    public void SetEpisodeSelectedHandler(Action<string> handler)
    {
        _episodeGraphRenderer.SetHandlers(handler);
    }

    public bool EnterChapter(string chapterId)
    {
        if (string.IsNullOrEmpty(chapterId))
        {
            Debug.LogWarning(
                "[EpisodeSelectionSystem] EnterChapter failed. chapterId is null or empty. " +
                "Check ChapterCardFactory and make sure each ChapterButtonCardModel has a valid ChapterId.");
            return false;
        }

        if (!_yarnEntriesByChapterId.TryGetValue(chapterId, out Dictionary<string, EpisodeYarnEntryData> yarnEntries))
        {
            string availableKeys = _yarnEntriesByChapterId.Count > 0
                ? string.Join(", ", _yarnEntriesByChapterId.Keys)
                : "<empty>";

            Debug.LogWarning(
                $"[EpisodeSelectionSystem] Yarn entries not found. " +
                $"requestedChapterId='{chapterId}', " +
                $"availableChapterIds=[{availableKeys}]. " +
                $"Empty map will be used." +
                $"Check ChapterCardFactory and make sure each ChapterButtonCardModel has a valid ChapterId.");
            
            yarnEntries = new Dictionary<string, EpisodeYarnEntryData>(StringComparer.Ordinal);
        }

        if (!_graphDataByChapterId.TryGetValue(chapterId, out EpisodeGraphData graphData))
        {
            //Debug.LogWarning($"[EpisodeSelectionSystem] Graph data not found. chapterId='{chapterId}'. Empty graph data will be used.");
            graphData = new EpisodeGraphData();
        }

        if (!_ruleDataByChapterId.TryGetValue(chapterId, out EpisodeProgressionRuleData ruleData))
        {
            //Debug.LogWarning($"[EpisodeSelectionSystem] Progression rule data not found. chapterId='{chapterId}'. Empty rule data will be used.");
            ruleData = new EpisodeProgressionRuleData();
        }
        
        _yarnEntryByEpisodeId = yarnEntries;
        _currentGraphData = graphData;
        _progressionRules = ruleData;

        return true;
    }

    public bool DrawEpisodeNodes()
    {
        SelectionState.ResetForChapter();

        _conditionEvaluator.RebuildAvailabilityState(_progressionRules);

        RenderGraphView();
        return true;
    }

    public void MarkEpisodeCompleted(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        SelectionState.MarkEpisodeCleared(episodeId);
        
        _conditionEvaluator.RebuildAvailabilityState(_progressionRules);
        RenderGraphView();
    }

    public bool MarkEpisodeSelected(string episodeId)
    {
        if (SelectionState.IsEpisodeLocked(episodeId))
            return false;

        SelectionState.SetSelectedEpisodeId(episodeId);
        RenderGraphView();

        return true;
    }

    private void RenderGraphView()
    {
        EpisodeGraphViewData viewData = _viewModelBuilder.Build(_currentGraphData);
        _episodeGraphRenderer.Render(viewData);
    }
}