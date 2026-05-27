public sealed class EpisodeSelectionController
{
    private readonly EpisodeSelectionRepository _repository;
    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;
    private readonly EpisodeGraphRenderer _episodeGraphRenderer;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;
    private readonly EpisodeGraphScrollController _scrollController;

    public EpisodeSelectionController(
        EpisodeSelectionRepository repository,
        EpisodeGraphViewModelBuilder viewModelBuilder,
        EpisodeGraphRenderer episodeGraphRenderer,
        EpisodeGraphLayoutOptions layoutOptions,
        EpisodeGraphScrollController scrollController)
    {
        _repository = repository;
        _viewModelBuilder = viewModelBuilder;
        _episodeGraphRenderer = episodeGraphRenderer;
        _layoutOptions = layoutOptions;
        _scrollController = scrollController;

        _episodeGraphRenderer.SetHandlers(RequestSelectEpisode);
    }

    public void RequestRender()
    {
        EpisodeSelectionSnapshot snapshot = _repository.ReadSnapshot();

        EpisodeGraphViewData viewData = _viewModelBuilder.Build(
            snapshot.GraphData,
            snapshot.RuntimeState,
            _layoutOptions);

        _episodeGraphRenderer.Render(viewData);

        string selected = snapshot.RuntimeState.SelectedEpisodeId;

        if (!string.IsNullOrEmpty(selected) && viewData.TryGetNode(selected, out EpisodeNodeViewData node))
            _scrollController.ScrollToPositionX(node.AnchoredPosition.x, 0.5f);
        else
            _scrollController.ScrollToLeft();
    }

    public bool TryGetSelectedEpisodeId(out string episodeId)
    {
        EpisodeSelectionSnapshot snapshot = _repository.ReadSnapshot();

        episodeId = snapshot.RuntimeState.SelectedEpisodeId;
        return !string.IsNullOrEmpty(episodeId);
    }

    public void RequestCompleteEpisode(string episodeId)
    {
        EpisodeSelectionSnapshot before = _repository.ReadSnapshot();

        EpisodeSelectionRuntimeState next = before.RuntimeState.Clone();
        next.CurrentEpisodeId = episodeId;
        next.SelectedEpisodeId = episodeId;
        next.ClearedEpisodeIds.Add(episodeId);

        _repository.CommitRuntimeState(next);
    }

    private void RequestSelectEpisode(string episodeId)
    {
        EpisodeSelectionSnapshot before = _repository.ReadSnapshot();

        EpisodeSelectionRuntimeState next = before.RuntimeState.Clone();
        next.SelectedEpisodeId = episodeId;

        _repository.CommitRuntimeState(next);

        EpisodeSelectionSnapshot after = _repository.ReadSnapshot();

        EpisodeGraphViewData viewData = _viewModelBuilder.Build(
            after.GraphData,
            after.RuntimeState,
            _layoutOptions);

        _episodeGraphRenderer.Render(viewData);
    }
}