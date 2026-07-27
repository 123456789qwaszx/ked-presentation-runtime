using Yarn.Unity;

public sealed class ScenarioNodeRunner
{
    private readonly EpisodePlayer _episodePlayer;

    public ScenarioNodeRunner(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }

    public YarnTask PlayNodeAsync(string nodeName)
        => _episodePlayer.StartGameAsync(nodeName);
}