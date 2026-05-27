using System;
using UnityEngine;

public sealed class EpisodeProgressionGraphDataBuilder
{
    public EpisodeGraphData Build(ChapterEpisodeProgressionSO progression)
    {
        EpisodeGraphData graphData = new EpisodeGraphData();

        if (progression == null)
            return graphData;

        AddMainNodes(progression, graphData);
        AddMainEdges(progression, graphData);
        AddAttachmentNodes(progression, graphData);

        return graphData;
    }

    private void AddMainNodes(
        ChapterEpisodeProgressionSO progression,
        EpisodeGraphData graphData)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null)
                continue;

            if (string.IsNullOrEmpty(node.EpisodeId))
                continue;

            graphData.Nodes.Add(new EpisodeGraphNodeData
            {
                Id = node.EpisodeId,
                Kind = node.Kind,
                Title = node.Title,
                IndexText = node.IndexText,
                DialogueEntryId = node.DialogueEntryId
            });
        }
    }

    private void AddMainEdges(
        ChapterEpisodeProgressionSO progression,
        EpisodeGraphData graphData)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null || node.NextOptions == null)
                continue;

            for (int j = 0; j < node.NextOptions.Count; j++)
            {
                EpisodeNextOption option = node.NextOptions[j];

                if (option == null)
                    continue;

                if (string.IsNullOrEmpty(node.EpisodeId))
                    continue;

                if (string.IsNullOrEmpty(option.TargetEpisodeId))
                    continue;

                graphData.Edges.Add(new EpisodeGraphEdgeData
                {
                    FromEpisodeId = node.EpisodeId,
                    ToEpisodeId = option.TargetEpisodeId
                });
            }
        }
    }

    private void AddAttachmentNodes(
        ChapterEpisodeProgressionSO progression,
        EpisodeGraphData graphData)
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

                if (string.IsNullOrEmpty(attachment.AttachmentId))
                    continue;

                graphData.Nodes.Add(new EpisodeGraphNodeData
                {
                    Id = attachment.AttachmentId,
                    Kind = EpisodeNodeKind.Attachment,
                    Title = attachment.Title,
                    IndexText = attachment.IndexText,
                    DialogueEntryId = attachment.DialogueEntryId,
                    ParentEpisodeId = attachment.ParentEpisodeId
                });

                if (!string.IsNullOrEmpty(attachment.ParentEpisodeId))
                {
                    graphData.Edges.Add(new EpisodeGraphEdgeData
                    {
                        FromEpisodeId = attachment.ParentEpisodeId,
                        ToEpisodeId = attachment.AttachmentId
                    });
                }
            }
        }
    }
}