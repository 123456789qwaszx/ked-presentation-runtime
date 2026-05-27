using System;
using System.Collections.Generic;

public sealed class EpisodeSelectionRuntimeState
{
    public string SelectedEpisodeId;
    public string CurrentEpisodeId;

    public HashSet<string> ClearedEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> ClearedChapterIds = new(StringComparer.Ordinal);
    
    public HashSet<string> LockedEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> VisibleEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> ReachableEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> Tokens = new(StringComparer.Ordinal);

    public Dictionary<string, bool> Flags = new(StringComparer.Ordinal);
    public Dictionary<string, int> Stats = new(StringComparer.Ordinal);

    public EpisodeSelectionRuntimeState Clone()
    {
        return new EpisodeSelectionRuntimeState
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