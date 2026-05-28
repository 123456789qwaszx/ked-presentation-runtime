using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeDialogueEntryData
{
    public string EpisodeId;
    public EpisodeNodeKind Kind;
    public string DialogueEntryId;
}

[Serializable]public sealed class EpisodeChapterRuntimeData
{
    public string ChapterId;
    public string DisplayName;
    public string StartEpisodeId;

    private readonly Dictionary<string, EpisodeDialogueEntryData> _dialogueByEpisodeId = new(StringComparer.Ordinal);

    public void AddDialogueEntry(EpisodeDialogueEntryData entry)
    {
        if (entry == null)
            return;

        if (string.IsNullOrEmpty(entry.EpisodeId))
            return;

        _dialogueByEpisodeId[entry.EpisodeId] = entry;
    }

    public string GetDialogueEntryId(string episodeId)
    {
        EpisodeDialogueEntryData entry = _dialogueByEpisodeId[episodeId];
        return entry.DialogueEntryId;
    }

    public EpisodeDialogueEntryData GetDialogueEntry(string episodeId)
    {
        return _dialogueByEpisodeId[episodeId];
    }
}