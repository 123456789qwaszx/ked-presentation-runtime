using System;
using System.Collections.Generic;

public sealed class EpisodeYarnEntryMapBuilder
{
    public Dictionary<string, Dictionary<string, EpisodeYarnEntryData>> Build(ChapterEpisodeProgressionCatalogSO catalog)
    {
        Dictionary<string, Dictionary<string, EpisodeYarnEntryData>> result = new();

        if (catalog == null)
            return result;

        foreach (KeyValuePair<string, ChapterEpisodeProgressionSO> pair in catalog.EnumerateProgressions())
        {
            string chapterId = pair.Key;
            ChapterEpisodeProgressionSO progression = pair.Value;
            
            if (progression == null || progression.Nodes == null)
                continue;

            result[chapterId] = BuildChapterEntries(progression);
        }

        return result;
    }

    private Dictionary<string, EpisodeYarnEntryData> BuildChapterEntries(ChapterEpisodeProgressionSO progression)
    {
        Dictionary<string, EpisodeYarnEntryData> result = new ();

        AddNodeEntries(progression, result);
        AddAttachmentEntries(progression, result);

        return result;
    }

    private void AddNodeEntries(ChapterEpisodeProgressionSO progression, Dictionary<string, EpisodeYarnEntryData> result)
    {
        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null)
                continue;

            AddEntry(
                result,
                node.EpisodeId,
                node.Kind,
                node.DialogueEntryId);
        }
    }

    private void AddAttachmentEntries(ChapterEpisodeProgressionSO progression, Dictionary<string, EpisodeYarnEntryData> result)
    {
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

                AddEntry(
                    result,
                    attachment.AttachmentId,
                    EpisodeNodeKind.Attachment,
                    attachment.DialogueEntryId);
            }
        }
    }

    private void AddEntry(Dictionary<string, EpisodeYarnEntryData> result, string episodeId, EpisodeNodeKind kind, string yarnNodeName)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        result[episodeId] = new EpisodeYarnEntryData
        {
            EpisodeId = episodeId,
            Kind = kind,
            YarnNodeName = yarnNodeName ?? string.Empty
        };
    }
}