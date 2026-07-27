using Yarn.Unity;

public sealed class ScenarioNodeRunner : INodePlayerV3
{
    private readonly EpisodePlayer _episodePlayer;

    public ScenarioNodeRunner(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }

    public YarnTask PlayNodeAsync(string nodeName)
        => _episodePlayer.StartGameAsync(nodeName);
}