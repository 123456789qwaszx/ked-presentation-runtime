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
    
    
    private void RenderSnapshot(EpisodeSelectionSnapshot snapshot)
    {
        EpisodeGraphViewData viewData = _viewModelBuilder.Build(snapshot.GraphData, snapshot.RuntimeState, _layoutOptions);
        
        _episodeGraphRenderer.Render(viewData);

        string selected = snapshot.RuntimeState.SelectedEpisodeId;

        if (!string.IsNullOrEmpty(selected) && viewData.TryGetNode(selected, out EpisodeNodeViewData node))
        {
            _scrollController.ScrollToPositionX(node.AnchoredPosition.x, 0.5f);
        }
        else
        {
            _scrollController.ScrollToLeft();
        }
    }
}