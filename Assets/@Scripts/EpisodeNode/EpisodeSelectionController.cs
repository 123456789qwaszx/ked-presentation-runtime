using System;

public sealed class EpisodeSelectionController
{
    public event Action<string> EpisodeRequested;

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

        RequestRender();
    }

    private void RequestSelectEpisode(string episodeId)
    {
        EpisodeSelectionSnapshot before = _repository.ReadSnapshot();

        if (before.RuntimeState.LockedEpisodeIds.Contains(episodeId))
            return;

        EpisodeSelectionRuntimeState next = before.RuntimeState.Clone();
        next.SelectedEpisodeId = episodeId;

        _repository.CommitRuntimeState(next);

        RequestRender();

        EpisodeRequested?.Invoke(episodeId);
    }
    
    public bool TryGetDialogueEntryId(
        string episodeId,
        out string dialogueEntryId)
    {
        dialogueEntryId = "";

        if (string.IsNullOrEmpty(episodeId))
            return false;

        EpisodeSelectionSnapshot snapshot = _repository.ReadSnapshot();

        if (snapshot.GraphData == null)
            return false;

        EpisodeGraphNodeData node = snapshot.GraphData.FindNode(episodeId);

        if (node == null)
            return false;

        if (string.IsNullOrWhiteSpace(node.DialogueEntryId))
            return false;

        dialogueEntryId = node.DialogueEntryId;
        return true;
    }
}