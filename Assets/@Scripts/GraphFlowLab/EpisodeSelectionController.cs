public sealed class EpisodeSelectionController
{
    private readonly EpisodeSelectionRepository _repository;
    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;
    private readonly EpisodeGraphView _view;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;

    public EpisodeSelectionController(
        EpisodeSelectionRepository repository,
        EpisodeGraphViewModelBuilder viewModelBuilder,
        EpisodeGraphView view,
        EpisodeGraphLayoutOptions layoutOptions)
    {
        _repository = repository;
        _viewModelBuilder = viewModelBuilder;
        _view = view;
        _layoutOptions = layoutOptions;

        _view.SetHandlers(
            RequestSelectEpisode,
            RequestOpenLink);
    }

    public void RequestRender()
    {
        EpisodeSelectionSnapshot snapshot = _repository.ReadSnapshot();
        RenderSnapshot(snapshot);
    }

    public void RequestSelectEpisode(string episodeId)
    {
        EpisodeSelectionSnapshot before = _repository.ReadSnapshot();

        EpisodeSelectionRuntimeState next = before.RuntimeState.Clone();
        next.SelectedEpisodeId = episodeId;

        _repository.CommitRuntimeState(next);

        EpisodeSelectionSnapshot after = _repository.ReadSnapshot();
        RenderSnapshot(after);
    }

    private void RequestOpenLink(
        string ownerEpisodeId,
        EpisodeNodeLinkSlot slot,
        EpisodeNodeLinkViewData link)
    {
        if (link == null || string.IsNullOrEmpty(link.TargetEpisodeId))
            return;

        RequestSelectEpisode(link.TargetEpisodeId);
    }

    private void RenderSnapshot(EpisodeSelectionSnapshot snapshot)
    {
        EpisodeGraphViewData viewData = _viewModelBuilder.Build(snapshot.GraphData, snapshot.RuntimeState, _layoutOptions);

        _view.Render(viewData);
    }
}