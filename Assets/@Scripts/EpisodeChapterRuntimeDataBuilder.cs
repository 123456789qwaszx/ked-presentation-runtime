public sealed class EpisodeChapterRuntimeDataBuilder
{
    public EpisodeChapterRuntimeData Build(ChapterEpisodeProgressionSO progression)
    {
        EpisodeChapterRuntimeData data = new EpisodeChapterRuntimeData();

        if (progression == null)
            return data;

        data.ChapterId = progression.ChapterId ?? "";
        data.DisplayName = progression.DisplayName ?? "";
        data.StartEpisodeId = progression.StartEpisodeId ?? "";

        AddNodeDialogueEntries(progression, data);
        AddAttachmentDialogueEntries(progression, data);

        return data;
    }

    private void AddNodeDialogueEntries(
        ChapterEpisodeProgressionSO progression,
        EpisodeChapterRuntimeData data)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null)
                continue;

            data.AddDialogueEntry(new EpisodeDialogueEntryData
            {
                EpisodeId = node.EpisodeId ?? "",
                Kind = node.Kind,
                DialogueEntryId = node.DialogueEntryId ?? ""
            });
        }
    }

    private void AddAttachmentDialogueEntries(
        ChapterEpisodeProgressionSO progression,
        EpisodeChapterRuntimeData data)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null || node.Attachments == null)
                continue;

            for (int j = 0; j < node.Attachments.Count; j++)
            {
                EpisodeAttachmentDefinition attachment = node.Attachments[j];

                if (attachment == null)
                    continue;

                data.AddDialogueEntry(new EpisodeDialogueEntryData
                {
                    EpisodeId = attachment.AttachmentId ?? "",
                    Kind = EpisodeNodeKind.Attachment,
                    DialogueEntryId = attachment.DialogueEntryId ?? ""
                });
            }
        }
    }
}