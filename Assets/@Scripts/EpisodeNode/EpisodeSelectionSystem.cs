using System;

public sealed class EpisodeSelectionSystem
{
    public event Action<string> EpisodeRequested;

    private readonly ChapterEpisodeProgressionCatalogSO _progressionCatalog;
    private readonly EpisodeProgressionGraphDataBuilder _graphDataBuilder;
    private readonly EpisodeConditionEvaluator _conditionEvaluator;

    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;
    private readonly EpisodeGraphRenderer _episodeGraphRenderer;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;
    private readonly EpisodeGraphScrollController _scrollController;

    private readonly EpisodeSelectionRuntimeState _runtimeState;

    private ChapterEpisodeProgressionSO _currentProgression;
    private EpisodeGraphData _currentGraphData;

    public EpisodeSelectionSystem(
        ChapterEpisodeProgressionCatalogSO progressionCatalog,
        EpisodeProgressionGraphDataBuilder graphDataBuilder,
        EpisodeConditionEvaluator conditionEvaluator,
        EpisodeSelectionRuntimeState runtimeState,
        EpisodeGraphViewModelBuilder viewModelBuilder,
        EpisodeGraphRenderer episodeGraphRenderer,
        EpisodeGraphLayoutOptions layoutOptions,
        EpisodeGraphScrollController scrollController)
    {
        _progressionCatalog = progressionCatalog;
        _graphDataBuilder = graphDataBuilder;
        _conditionEvaluator = conditionEvaluator;
        _runtimeState = runtimeState;

        _viewModelBuilder = viewModelBuilder;
        _episodeGraphRenderer = episodeGraphRenderer;
        _layoutOptions = layoutOptions;
        _scrollController = scrollController;

        _episodeGraphRenderer.SetHandlers(RequestSelectEpisode);
    }

    public bool DrawEpisodeNodes(int chapterId)
    {
        if (_progressionCatalog == null)
            return false;

        if (!_progressionCatalog.TryGetProgression(
                chapterId,
                out ChapterEpisodeProgressionSO progression))
        {
            return false;
        }

        _currentProgression = progression;
        _currentGraphData = _graphDataBuilder.Build(progression);

        _conditionEvaluator.BindProgression(progression);

        InitializeRuntimeStateForChapter(chapterId, progression);

        RequestRender();
        return true;
    }

    private void RequestRender()
    {
        if (_currentGraphData == null)
            return;

        EpisodeGraphViewData viewData = _viewModelBuilder.Build(
            _currentGraphData,
            _runtimeState,
            _layoutOptions);

        _episodeGraphRenderer.Render(viewData);

        string selected = _runtimeState.SelectedEpisodeId;

        if (!string.IsNullOrEmpty(selected) &&
            viewData.TryGetNode(selected, out EpisodeNodeViewData node))
        {
            _scrollController.ScrollToPositionX(node.AnchoredPosition.x, 0.5f);
        }
        else
        {
            _scrollController.ScrollToLeft();
        }
    }

    public bool TryGetSelectedEpisodeId(out string episodeId)
    {
        episodeId = _runtimeState.SelectedEpisodeId;
        return !string.IsNullOrEmpty(episodeId);
    }

    public void RequestCompleteEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        _runtimeState.CurrentEpisodeId = episodeId;
        _runtimeState.SelectedEpisodeId = episodeId;
        _runtimeState.ClearedEpisodeIds.Add(episodeId);

        _conditionEvaluator.RebuildAvailabilityState();

        RequestRender();
    }

    public bool TryGetDialogueEntryId(string episodeId, out string dialogueEntryId)
    {
        dialogueEntryId = "";

        if (string.IsNullOrEmpty(episodeId))
            return false;

        if (_currentGraphData == null)
            return false;

        EpisodeGraphNodeData node = _currentGraphData.FindNode(episodeId);

        if (node == null)
            return false;

        if (string.IsNullOrWhiteSpace(node.DialogueEntryId))
            return false;

        dialogueEntryId = node.DialogueEntryId;
        return true;
    }

    private void RequestSelectEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        if (_runtimeState.LockedEpisodeIds.Contains(episodeId))
            return;

        _runtimeState.SelectedEpisodeId = episodeId;

        RequestRender();

        EpisodeRequested?.Invoke(episodeId);
    }

    private void InitializeRuntimeStateForChapter(
        int chapterId,
        ChapterEpisodeProgressionSO progression)
    {
        _runtimeState.CurrentChapterId = chapterId;

        _runtimeState.SelectedEpisodeId = progression.StartEpisodeId;
        _runtimeState.CurrentEpisodeId = progression.StartEpisodeId;

        _runtimeState.VisibleEpisodeIds.Clear();
        _runtimeState.LockedEpisodeIds.Clear();
        _runtimeState.ReachableEpisodeIds.Clear();

        if (!string.IsNullOrEmpty(progression.StartEpisodeId))
            _runtimeState.ReachableEpisodeIds.Add(progression.StartEpisodeId);

        _conditionEvaluator.RebuildAvailabilityState();
    }
}