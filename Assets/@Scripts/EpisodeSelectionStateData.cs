
using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeSelectionStateData
{
    public string SelectedEpisodeId;
    public string CurrentEpisodeId;

    public HashSet<string> ClearedEpisodeIds = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> ClearedChapterIds = new HashSet<string>(StringComparer.Ordinal);

    public HashSet<string> LockedEpisodeIds = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> VisibleEpisodeIds = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> ReachableEpisodeIds = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> Tokens = new HashSet<string>(StringComparer.Ordinal);

    public Dictionary<string, bool> Flags = new Dictionary<string, bool>(StringComparer.Ordinal);
    public Dictionary<string, int> Stats = new Dictionary<string, int>(StringComparer.Ordinal);

    public void ResetForChapter(string startEpisodeId)
    {
        SelectedEpisodeId = "";
        CurrentEpisodeId = "";

        VisibleEpisodeIds.Clear();
        LockedEpisodeIds.Clear();
        ReachableEpisodeIds.Clear();

        if (string.IsNullOrEmpty(startEpisodeId))
            return;

        SelectedEpisodeId = startEpisodeId;
        CurrentEpisodeId = startEpisodeId;
        ReachableEpisodeIds.Add(startEpisodeId);
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

    public EpisodeSelectionStateData Clone()
    {
        return new EpisodeSelectionStateData
        {
            SelectedEpisodeId = SelectedEpisodeId,
            CurrentEpisodeId = CurrentEpisodeId,

            ClearedEpisodeIds = new HashSet<string>(ClearedEpisodeIds, StringComparer.Ordinal),
            ClearedChapterIds = new HashSet<string>(ClearedChapterIds, StringComparer.Ordinal),

            LockedEpisodeIds = new HashSet<string>(LockedEpisodeIds, StringComparer.Ordinal),
            VisibleEpisodeIds = new HashSet<string>(VisibleEpisodeIds, StringComparer.Ordinal),
            ReachableEpisodeIds = new HashSet<string>(ReachableEpisodeIds, StringComparer.Ordinal),
            Tokens = new HashSet<string>(Tokens, StringComparer.Ordinal),

            Flags = new Dictionary<string, bool>(Flags, StringComparer.Ordinal),
            Stats = new Dictionary<string, int>(Stats, StringComparer.Ordinal)
        };
    }
}