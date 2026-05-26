public sealed class EpisodeSelectionRepository
{
    private readonly EpisodeGraphData _graphData;
    private EpisodeSelectionRuntimeState _runtimeState;

    public EpisodeSelectionRepository(EpisodeGraphData graphData, EpisodeSelectionRuntimeState initialState)
    {
        _graphData = graphData;
        _runtimeState = initialState != null ? initialState.Clone() : new EpisodeSelectionRuntimeState();
    }

    public EpisodeSelectionSnapshot ReadSnapshot()
    {
        return new EpisodeSelectionSnapshot(
            _graphData,
            _runtimeState.Clone());
    }

    public void CommitRuntimeState(EpisodeSelectionRuntimeState nextState)
    {
        _runtimeState = nextState;
    }
}

public readonly struct EpisodeSelectionSnapshot
{
    public readonly EpisodeGraphData GraphData;
    public readonly EpisodeSelectionRuntimeState RuntimeState;

    public EpisodeSelectionSnapshot(EpisodeGraphData graphData, EpisodeSelectionRuntimeState runtimeState)
    {
        GraphData = graphData;
        RuntimeState = runtimeState;
    }
}