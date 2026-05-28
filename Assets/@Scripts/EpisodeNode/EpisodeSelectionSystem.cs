using System;

public sealed class EpisodeSelectionSystem
{
    public event Action<string> EpisodeRequested;

    private readonly EpisodeSelectionRuntimeModel _runtimeModel;
    private readonly EpisodeConditionEvaluator _conditionEvaluator;

    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;
    private readonly EpisodeGraphRenderer _episodeGraphRenderer;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;
    private readonly EpisodeGraphScrollController _scrollController;

    public EpisodeSelectionSystem(
        EpisodeSelectionRuntimeModel runtimeModel,
        EpisodeConditionEvaluator conditionEvaluator,
        EpisodeGraphViewModelBuilder viewModelBuilder,
        EpisodeGraphRenderer episodeGraphRenderer,
        EpisodeGraphLayoutOptions layoutOptions,
        EpisodeGraphScrollController scrollController)
    {
        _runtimeModel = runtimeModel;
        _conditionEvaluator = conditionEvaluator;

        _viewModelBuilder = viewModelBuilder;
        _episodeGraphRenderer = episodeGraphRenderer;
        _layoutOptions = layoutOptions;
        _scrollController = scrollController;

        _episodeGraphRenderer.SetHandlers(RequestSelectEpisode);
    }

    public bool DrawEpisodeNodes(int chapterId)
    {
        if (_runtimeModel == null)
            return false;

        if (!_runtimeModel.OpenChapter(chapterId))
            return false;

        _conditionEvaluator.RebuildAvailabilityState();

        RenderCurrentState();
        return true;
    }

    private void RenderCurrentState()
    {
        if (_runtimeModel == null)
            return;

        if (_runtimeModel.CurrentGraphData == null)
            return;

        EpisodeGraphViewData viewData = _viewModelBuilder.Build(
            _runtimeModel.CurrentGraphData,
            _runtimeModel,
            _layoutOptions);

        _episodeGraphRenderer.Render(viewData);

        string selected = _runtimeModel.SelectedEpisodeId;

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
        episodeId = "";

        if (_runtimeModel == null)
            return false;

        return _runtimeModel.TryGetSelectedEpisodeId(out episodeId);
    }

    public void RequestCompleteEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        if (_runtimeModel == null)
            return;

        _runtimeModel.CompleteEpisode(episodeId);

        _conditionEvaluator.RebuildAvailabilityState();

        RenderCurrentState();
    }

    public bool TryGetDialogueEntryId(
        string episodeId,
        out string dialogueEntryId)
    {
        dialogueEntryId = "";

        if (_runtimeModel == null)
            return false;

        return _runtimeModel.TryGetDialogueEntryId(
            episodeId,
            out dialogueEntryId);
    }

    private void RequestSelectEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        if (_runtimeModel == null)
            return;

        if (_runtimeModel.IsEpisodeLocked(episodeId))
            return;

        _runtimeModel.SelectEpisode(episodeId);

        RenderCurrentState();

        EpisodeRequested?.Invoke(episodeId);
    }
}