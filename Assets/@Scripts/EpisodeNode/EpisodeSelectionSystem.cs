using System;

public sealed class EpisodeSelectionSystem
{
    private readonly EpisodeSelectionRuntimeModel _runtimeModel;
    private readonly EpisodeConditionEvaluator _conditionEvaluator;

    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;
    private readonly EpisodeGraphRenderer _episodeGraphRenderer;

    public EpisodeSelectionSystem(
        EpisodeSelectionRuntimeModel runtimeModel,
        EpisodeConditionEvaluator conditionEvaluator,
        EpisodeGraphViewModelBuilder viewModelBuilder,
        EpisodeGraphRenderer episodeGraphRenderer)
    {
        _runtimeModel = runtimeModel;
        _conditionEvaluator = conditionEvaluator;

        _viewModelBuilder = viewModelBuilder;
        _episodeGraphRenderer = episodeGraphRenderer;
    }

    public EpisodeSelectionStateData SelectionState => _runtimeModel.State;
    public EpisodeChapterRuntimeData CurrentChapter => _runtimeModel.CurrentChapter;
    public EpisodeGraphData CurrentGraphData => _runtimeModel.CurrentGraphData;
    public EpisodeProgressionRuleData ProgressionRules => _runtimeModel.ProgressionRules;
    
    public string GetSelectedDialogueEntryId() => CurrentChapter.GetDialogueEntryId(SelectionState.SelectedEpisodeId);
    public void SetEpisodeSelectedHandler(Action<string> handler) => _episodeGraphRenderer.SetHandlers(handler);
    
    public bool DrawEpisodeNodes(int chapterId)
    {
        if (!_runtimeModel.OpenChapter(chapterId))
            return false;

        _conditionEvaluator.RebuildAvailabilityState();

        RenderCurrentState();
        return true;
    }

    public void RequestCompleteEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        SelectionState.CompleteEpisode(episodeId);

        _conditionEvaluator.RebuildAvailabilityState();

        RenderCurrentState();
    }

    public void RenderCurrentState()
    {
        EpisodeGraphViewData viewData = _viewModelBuilder.Build();
        _episodeGraphRenderer.Render(viewData);
    }
}