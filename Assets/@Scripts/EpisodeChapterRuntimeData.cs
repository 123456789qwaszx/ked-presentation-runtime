using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeDialogueEntryData
{
    public string EpisodeId;
    public EpisodeNodeKind Kind;
    public string DialogueEntryId;
}

[Serializable]
public sealed class EpisodeChapterRuntimeData
{
    public string ChapterId;
    public string DisplayName;
    public string StartEpisodeId;

    private readonly Dictionary<string, EpisodeDialogueEntryData> _dialogueByEpisodeId =
        new Dictionary<string, EpisodeDialogueEntryData>(StringComparer.Ordinal);

    public void AddDialogueEntry(EpisodeDialogueEntryData entry)
    {
        if (entry == null)
            return;

        if (string.IsNullOrEmpty(entry.EpisodeId))
            return;

        _dialogueByEpisodeId[entry.EpisodeId] = entry;
    }

    public bool TryGetDialogueEntry(
        string episodeId,
        out EpisodeDialogueEntryData entry)
    {
        entry = null;

        if (string.IsNullOrEmpty(episodeId))
            return false;

        return _dialogueByEpisodeId.TryGetValue(episodeId, out entry);
    }

    public bool TryGetDialogueEntryId(
        string episodeId,
        out string dialogueEntryId)
    {
        dialogueEntryId = "";

        if (!TryGetDialogueEntry(episodeId, out EpisodeDialogueEntryData entry))
            return false;

        if (string.IsNullOrWhiteSpace(entry.DialogueEntryId))
            return false;

        dialogueEntryId = entry.DialogueEntryId;
        return true;
    }
}