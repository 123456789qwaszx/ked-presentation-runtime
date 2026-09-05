using Ked.Progression;

public sealed class SceneRunContext
{
    public ChapterProgression Chapter { get; }
    public ProgressionState EntryState { get; }

    public string RootEpisodeId { get; }
    public string CurrentEpisodeId { get; set; }

    public bool IsNewSession { get; }
    public SavedLoadPlan LoadPlan { get; }

    public SceneRunPhase Phase { get; set; } =
        SceneRunPhase.None;

    public SceneRunContext(
        ChapterProgression chapter,
        ProgressionState entryState,
        bool isNewSession,
        SavedLoadPlan loadPlan)
    {
        Chapter = chapter;
        EntryState = entryState;
        RootEpisodeId = entryState.CurrentEpisodeId;
        CurrentEpisodeId = RootEpisodeId;
        IsNewSession = isNewSession;
        LoadPlan = loadPlan;
    }
}