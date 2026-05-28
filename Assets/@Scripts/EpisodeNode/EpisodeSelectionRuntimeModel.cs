using System;
using System.Collections.Generic;

public sealed class EpisodeSelectionRuntimeModel
{
    private readonly ChapterEpisodeProgressionCatalogSO _progressionCatalog;
    private readonly EpisodeProgressionGraphDataBuilder _graphDataBuilder;

    public int CurrentChapterId = -1;

    public string SelectedEpisodeId;
    public string CurrentEpisodeId;

    public ChapterEpisodeProgressionSO CurrentProgression { get; private set; }
    public EpisodeGraphData CurrentGraphData { get; private set; }

    public HashSet<string> ClearedEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> ClearedChapterIds = new(StringComparer.Ordinal);

    public HashSet<string> LockedEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> VisibleEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> ReachableEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> Tokens = new(StringComparer.Ordinal);

    public Dictionary<string, bool> Flags = new(StringComparer.Ordinal);
    public Dictionary<string, int> Stats = new(StringComparer.Ordinal);
    

    public EpisodeSelectionRuntimeModel(
        ChapterEpisodeProgressionCatalogSO progressionCatalog,
        EpisodeProgressionGraphDataBuilder graphDataBuilder)
    {
        _progressionCatalog = progressionCatalog;
        _graphDataBuilder = graphDataBuilder;
    }

    public bool OpenChapter(int chapterId)
    {
        if (_progressionCatalog == null)
            return false;

        if (_graphDataBuilder == null)
            return false;

        if (!_progressionCatalog.TryGetProgression(
                chapterId,
                out ChapterEpisodeProgressionSO progression))
        {
            return false;
        }

        CurrentChapterId = chapterId;
        CurrentProgression = progression;
        CurrentGraphData = _graphDataBuilder.Build(progression);

        InitializeForCurrentProgression();
        return true;
    }

    public void SelectEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        SelectedEpisodeId = episodeId;
    }

    public void CompleteEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        CurrentEpisodeId = episodeId;
        SelectedEpisodeId = episodeId;
        ClearedEpisodeIds.Add(episodeId);
    }

    public bool IsEpisodeLocked(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return true;

        return LockedEpisodeIds.Contains(episodeId);
    }

    public bool TryGetSelectedEpisodeId(out string episodeId)
    {
        episodeId = SelectedEpisodeId;
        return !string.IsNullOrEmpty(episodeId);
    }

    public bool TryFindNode(
        string episodeId,
        out EpisodeGraphNodeData node)
    {
        node = null;

        if (string.IsNullOrEmpty(episodeId))
            return false;

        if (CurrentGraphData == null)
            return false;

        node = CurrentGraphData.FindNode(episodeId);
        return node != null;
    }

    public bool TryGetDialogueEntryId(
        string episodeId,
        out string dialogueEntryId)
    {
        dialogueEntryId = "";

        if (!TryFindNode(episodeId, out EpisodeGraphNodeData node))
            return false;

        if (string.IsNullOrWhiteSpace(node.DialogueEntryId))
            return false;

        dialogueEntryId = node.DialogueEntryId;
        return true;
    }

    private void InitializeForCurrentProgression()
    {
        SelectedEpisodeId = "";
        CurrentEpisodeId = "";

        VisibleEpisodeIds.Clear();
        LockedEpisodeIds.Clear();
        ReachableEpisodeIds.Clear();

        if (CurrentProgression == null)
            return;

        SelectedEpisodeId = CurrentProgression.StartEpisodeId;
        CurrentEpisodeId = CurrentProgression.StartEpisodeId;

        if (!string.IsNullOrEmpty(CurrentProgression.StartEpisodeId))
            ReachableEpisodeIds.Add(CurrentProgression.StartEpisodeId);
    }

    public EpisodeSelectionRuntimeModel CloneRuntimeValuesOnly()
    {
        EpisodeSelectionRuntimeModel clone = new EpisodeSelectionRuntimeModel(
            _progressionCatalog,
            _graphDataBuilder)
        {
            CurrentChapterId = CurrentChapterId,
            SelectedEpisodeId = SelectedEpisodeId,
            CurrentEpisodeId = CurrentEpisodeId,
            CurrentProgression = CurrentProgression,
            CurrentGraphData = CurrentGraphData,

            ClearedEpisodeIds = new HashSet<string>(ClearedEpisodeIds, StringComparer.Ordinal),
            ClearedChapterIds = new HashSet<string>(ClearedChapterIds, StringComparer.Ordinal),
            LockedEpisodeIds = new HashSet<string>(LockedEpisodeIds, StringComparer.Ordinal),
            VisibleEpisodeIds = new HashSet<string>(VisibleEpisodeIds, StringComparer.Ordinal),
            ReachableEpisodeIds = new HashSet<string>(ReachableEpisodeIds, StringComparer.Ordinal),
            Tokens = new HashSet<string>(Tokens, StringComparer.Ordinal),

            Flags = new Dictionary<string, bool>(Flags, StringComparer.Ordinal),
            Stats = new Dictionary<string, int>(Stats, StringComparer.Ordinal)
        };

        return clone;
    }
}