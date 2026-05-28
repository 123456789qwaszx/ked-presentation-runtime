using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeSelectionStateData
{
    public string SelectedEpisodeId = "";
    public string CurrentEpisodeId = "";

    public HashSet<string> ClearedEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> ClearedChapterIds = new(StringComparer.Ordinal);

    public HashSet<string> LockedEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> VisibleEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> ReachableEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> Tokens = new(StringComparer.Ordinal);

    public Dictionary<string, bool> Flags = new(StringComparer.Ordinal);
    public Dictionary<string, int> Stats = new(StringComparer.Ordinal);

    public EpisodeSelectionStateData()
    {
    }

    public void ResetForChapter()
    {
        SelectedEpisodeId = "";
        CurrentEpisodeId = "";

        VisibleEpisodeIds.Clear();
        LockedEpisodeIds.Clear();
        ReachableEpisodeIds.Clear();
    }

    public void SetSelectedEpisodeId(string episodeId)
    {
        SelectedEpisodeId = episodeId;
    }

    public void MarkEpisodeCleared(string episodeId)
    {
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
    
    public bool ShouldShowEpisode(string episodeId)
    {
        if (VisibleEpisodeIds.Count != 0)
            return true;

        return VisibleEpisodeIds.Contains(episodeId);
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