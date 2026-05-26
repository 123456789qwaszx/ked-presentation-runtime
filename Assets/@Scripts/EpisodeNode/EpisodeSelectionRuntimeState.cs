using System;
using System.Collections.Generic;

public sealed class EpisodeSelectionRuntimeState
{
    public string SelectedEpisodeId;
    public string CurrentEpisodeId;

    public HashSet<string> ClearedEpisodeIds = new(StringComparer.Ordinal);
    public HashSet<string> LockedEpisodeIds = new(StringComparer.Ordinal);

    public EpisodeSelectionRuntimeState Clone()
    {
        return new EpisodeSelectionRuntimeState
        {
            SelectedEpisodeId = SelectedEpisodeId,
            CurrentEpisodeId = CurrentEpisodeId,
            ClearedEpisodeIds = new HashSet<string>(ClearedEpisodeIds, StringComparer.Ordinal),
            LockedEpisodeIds = new HashSet<string>(LockedEpisodeIds, StringComparer.Ordinal)
        };
    }
}