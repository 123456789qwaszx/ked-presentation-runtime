using System;
using System.Collections.Generic;

public sealed class EpisodeYarnEntryMapBuilder
{
    public Dictionary<string, Dictionary<string, EpisodeYarnEntryData>> Build(
        ChapterEpisodeProgressionCatalogSO catalog)
    {
        Dictionary<string, Dictionary<string, EpisodeYarnEntryData>> result =
            new Dictionary<string, Dictionary<string, EpisodeYarnEntryData>>();

        if (catalog == null)
            return result;

        AddChapterEntries(catalog, result);

        return result;
    }

    private void AddChapterEntries(
        ChapterEpisodeProgressionCatalogSO catalog,
        Dictionary<string, Dictionary<string, EpisodeYarnEntryData>> result)
    {
        foreach (KeyValuePair<string, ChapterEpisodeProgressionSO> pair in catalog.EnumerateProgressions())
        {
            string chapterId = pair.Key;
            ChapterEpisodeProgressionSO progression = pair.Value;

            if (progression == null)
                continue;

            result[chapterId] = BuildChapterEntries(progression);
        }
    }

    private Dictionary<string, EpisodeYarnEntryData> BuildChapterEntries(
        ChapterEpisodeProgressionSO progression)
    {
        Dictionary<string, EpisodeYarnEntryData> result =
            new Dictionary<string, EpisodeYarnEntryData>(StringComparer.Ordinal);

        if (progression == null || progression.Nodes == null)
            return result;

        AddNodeEntries(progression, result);
        AddAttachmentEntries(progression, result);

        return result;
    }

    private void AddNodeEntries(
        ChapterEpisodeProgressionSO progression,
        Dictionary<string, EpisodeYarnEntryData> result)
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

    private void AddAttachmentEntries(
        ChapterEpisodeProgressionSO progression,
        Dictionary<string, EpisodeYarnEntryData> result)
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

    private void AddEntry(
        Dictionary<string, EpisodeYarnEntryData> result,
        string episodeId,
        EpisodeNodeKind kind,
        string yarnNodeName)
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