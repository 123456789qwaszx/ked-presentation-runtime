using System;

public sealed class EpisodeSelectionSystem
{
    private readonly EpisodeSelectionRuntimeContext _runtimeContext;
    private readonly EpisodeConditionEvaluator _conditionEvaluator;

    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;
    private readonly EpisodeGraphRenderer _episodeGraphRenderer;

    public EpisodeSelectionSystem(
        EpisodeSelectionRuntimeContext runtimeModel,
        EpisodeConditionEvaluator conditionEvaluator,
        EpisodeGraphViewModelBuilder viewModelBuilder,
        EpisodeGraphRenderer episodeGraphRenderer)
    {
        _runtimeContext = runtimeModel;
        _conditionEvaluator = conditionEvaluator;

        _viewModelBuilder = viewModelBuilder;
        _episodeGraphRenderer = episodeGraphRenderer;
    }

    public EpisodeSelectionStateData SelectionState => _runtimeContext.State;
    public EpisodeGraphData CurrentGraphData => _runtimeContext.CurrentGraphData;
    public EpisodeProgressionRuleData ProgressionRules => _runtimeContext.ProgressionRules;

    public String SelectedEpisodeId => SelectionState.SelectedEpisodeId ?? String.Empty;
    
    public string GetYarnNodeName()
    {
        string yarnNodeName = "yarnNodeFallback";

        if (!_runtimeContext.YarnEntryByEpisodeId.TryGetValue(SelectedEpisodeId, out EpisodeYarnEntryData entry))
            return yarnNodeName;

        yarnNodeName = entry.YarnNodeName;
        return yarnNodeName;
    }
    
    public void SetEpisodeSelectedHandler(Action<string> handler) => _episodeGraphRenderer.SetHandlers(handler);
    
    public bool DrawEpisodeNodes(int chapterId)
    {
        if (!_runtimeContext.OpenChapter(chapterId))
            return false;

        _conditionEvaluator.RebuildAvailabilityState();

        RefreshGraphView();
        return true;
    }

    public void MarkEpisodeCompleted(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        SelectionState.MarkEpisodeCleared(episodeId);

        _conditionEvaluator.RebuildAvailabilityState();

        RefreshGraphView();
    }
    
    public bool TrySetSelectedEpisode(string episodeId)
    {
        if (SelectionState.IsEpisodeLocked(episodeId))
            return false;

        SelectionState.SetSelectedEpisodeId(episodeId);
        RefreshGraphView();

        return true;
    }
    
    private void RefreshGraphView()
    {
        EpisodeGraphViewData viewData = _viewModelBuilder.Build();
        _episodeGraphRenderer.Render(viewData);
    }
}