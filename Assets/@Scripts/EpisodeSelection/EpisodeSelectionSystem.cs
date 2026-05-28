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

    public Dictionary<string, EpisodeYarnEntryData> YarnEntryByEpisodeId { get; private set; }
    public EpisodeGraphData CurrentGraphData { get; private set; }
    public EpisodeProgressionRuleData ProgressionRules { get; private set; }
    public EpisodeSelectionStateData SelectionState { get; private set; }
    
    public string GetYarnNodeName(string episodeId)
    {
        const string fallback = "yarnNodeFallback";

        if (!YarnEntryByEpisodeId.TryGetValue(episodeId, out EpisodeYarnEntryData entry))
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
                $"Empty map will be used.");
            
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
        
        YarnEntryByEpisodeId = yarnEntries;
        CurrentGraphData = graphData;
        ProgressionRules = ruleData;

        return true;
    }

    public bool PresentCurrentChapterEpisodes()
    {
        SelectionState.ResetForChapter();

        _conditionEvaluator.RebuildAvailabilityState(ProgressionRules);

        RefreshGraphView();
        return true;
    }

    public void MarkEpisodeCompleted(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        SelectionState.MarkEpisodeCleared(episodeId);
        
        //_conditionEvaluator.RebuildAvailabilityState(ProgressionRules);
        //RefreshGraphView();
    }

    public bool MarkEpisodeSelected(string episodeId)
    {
        if (SelectionState.IsEpisodeLocked(episodeId))
            return false;

        SelectionState.SetSelectedEpisodeId(episodeId);
        RefreshGraphView();

        return true;
    }

    private void RefreshGraphView()
    {
        EpisodeGraphViewData viewData = _viewModelBuilder.Build(CurrentGraphData);
        _episodeGraphRenderer.Render(viewData);
    }
}