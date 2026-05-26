public sealed class EpisodeSelectionController
{
    private readonly EpisodeSelectionRepository _repository;
    private readonly EpisodeGraphViewModelBuilder _viewModelBuilder;
    private readonly EpisodeGraphRenderer _episodeGraphRenderer;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;

    public EpisodeSelectionController(
        EpisodeSelectionRepository repository,
        EpisodeGraphViewModelBuilder viewModelBuilder,
        EpisodeGraphRenderer episodeGraphRenderer,
        EpisodeGraphLayoutOptions layoutOptions)
    {
        _repository = repository;
        _viewModelBuilder = viewModelBuilder;
        _episodeGraphRenderer = episodeGraphRenderer;
        _layoutOptions = layoutOptions;
        
        _episodeGraphRenderer.SetHandlers(RequestSelectEpisode);
    }

    public void RequestRender()
    {
        EpisodeSelectionSnapshot snapshot = _repository.ReadSnapshot();
        EpisodeGraphViewData viewData = _viewModelBuilder.Build(snapshot.GraphData, snapshot.RuntimeState, _layoutOptions);
        _episodeGraphRenderer.Render(viewData);
    }

    public void RequestSelectEpisode(string episodeId)
    {
        EpisodeSelectionSnapshot before = _repository.ReadSnapshot();

        EpisodeSelectionRuntimeState next = before.RuntimeState.Clone();
        next.SelectedEpisodeId = episodeId;

        _repository.CommitRuntimeState(next);

        EpisodeSelectionSnapshot after = _repository.ReadSnapshot();
        EpisodeGraphViewData viewData = _viewModelBuilder.Build(after.GraphData, after.RuntimeState, _layoutOptions);
        _episodeGraphRenderer.Render(viewData);
    }
}