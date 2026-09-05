using System.Collections.Generic;
using Ked.Progression;

public sealed class SceneRunContext
{
    public ChapterProgression Chapter { get; }
    public ProgressionState EntryState { get; }

    public string RootEpisodeId { get; }
    public string CurrentEpisodeId { get; internal set; }

    public SavedLoadPlan LoadPlan { get; }

    public SceneRunPhase Phase { get; internal set; } = SceneRunPhase.None;

    public bool ReplayRequested { get; internal set; }

    public EpisodeNode RootEpisode => GetEpisode(RootEpisodeId);
    public EpisodeNode CurrentEpisode => GetEpisode(CurrentEpisodeId);

    public IReadOnlyList<CommittedChoice> PendingPath =>
        History.CreatePendingPath();

    internal ScenePendingHistory History { get; } = new();

    public SceneRunContext(
        ChapterProgression chapter,
        ProgressionState entryState,
        SavedLoadPlan loadPlan = null)
    {
        Chapter = chapter;
        EntryState = entryState;

        RootEpisodeId = entryState.CurrentEpisodeId;
        CurrentEpisodeId = RootEpisodeId;

        LoadPlan = loadPlan;
    }

    private EpisodeNode GetEpisode(string episodeId)
    {
        Chapter.TryGetNode(episodeId, out EpisodeNode episode);
        return episode;
    }
}