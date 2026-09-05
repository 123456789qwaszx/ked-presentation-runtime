using Ked.Progression;

public sealed class SceneRunContext
{
    public ChapterProgression Chapter { get; }
    public ProgressionState EntryState { get; }

    public string RootEpisodeId { get; }
    public string CurrentEpisodeId { get; internal set; }

    public bool IsNewSession { get; }
    public SavedLoadPlan LoadPlan { get; }

    public SceneRunPhase Phase { get; internal set; } = SceneRunPhase.None;

    public EpisodeNode RootEpisode => GetEpisode(RootEpisodeId);
    public EpisodeNode CurrentEpisode => GetEpisode(CurrentEpisodeId);

    public SceneRunContext(
        ChapterProgression chapter,
        ProgressionState entryState,
        bool isNewSession,
        SavedLoadPlan loadPlan = null)
    {
        Chapter = chapter;
        EntryState = entryState;

        RootEpisodeId = entryState.CurrentEpisodeId;
        CurrentEpisodeId = RootEpisodeId;

        IsNewSession = isNewSession;
        LoadPlan = loadPlan;
    }

    private EpisodeNode GetEpisode(string episodeId)
    {
        Chapter.TryGetNode(episodeId, out EpisodeNode episode);
        return episode;
    }
}