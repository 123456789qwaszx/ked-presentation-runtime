public sealed class EpisodeSelectionFactory
{
    private readonly EpisodeProgressionGraphDataBuilder _graphDataBuilder = new();

    public EpisodeSelectionRepository CreateRepository(
        ChapterEpisodeProgressionSO progression,
        EpisodeSelectionRuntimeState savedState)
    {
        EpisodeGraphData graphData = _graphDataBuilder.Build(progression);

        EpisodeSelectionRuntimeState state =
            savedState != null ? savedState.Clone() : new EpisodeSelectionRuntimeState();

        InitializeState(progression, state);

        return new EpisodeSelectionRepository(graphData, state);
    }

    private void InitializeState(
        ChapterEpisodeProgressionSO progression,
        EpisodeSelectionRuntimeState state)
    {
        if (progression == null)
            return;

        if (string.IsNullOrEmpty(state.CurrentEpisodeId))
            state.CurrentEpisodeId = progression.StartEpisodeId;

        if (string.IsNullOrEmpty(state.SelectedEpisodeId))
            state.SelectedEpisodeId = progression.StartEpisodeId;

        if (!string.IsNullOrEmpty(progression.StartEpisodeId))
            state.ReachableEpisodeIds.Add(progression.StartEpisodeId);
    }
}