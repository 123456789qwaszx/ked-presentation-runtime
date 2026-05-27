using System.Collections.Generic;
using System;

public enum EpisodeNodeKind
{
    Main,
    Attachment
}

public sealed class EpisodeGraphNodeData
{
    public EpisodeNodeKind Kind;
    public string Id;
    
    public string Title;
    public string IndexText;

    public string DialogueEntryId;

    public string ParentEpisodeId;
}

public sealed class EpisodeGraphEdgeData
{
    public string FromEpisodeId;
    public string ToEpisodeId;
}

public sealed class EpisodeGraphData
{
    public List<EpisodeGraphNodeData> Nodes = new();
    public List<EpisodeGraphEdgeData> Edges = new();

    public EpisodeGraphNodeData FindNode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return null;

        for (int i = 0; i < Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = Nodes[i];

            if (node != null && string.Equals(node.Id, episodeId, StringComparison.Ordinal))
                return node;
        }

        return null;
    }

    public string GetFirstMainEpisodeId()
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = Nodes[i];

            if (node != null && node.Kind == EpisodeNodeKind.Main)
                return node.Id;
        }

        if (Nodes.Count > 0 && Nodes[0] != null)
            return Nodes[0].Id;

        return "";
    }

    public string FindNextMainEpisodeId(string currentEpisodeId)
    {
        if (string.IsNullOrEmpty(currentEpisodeId))
            return "";

        for (int i = 0; i < Edges.Count; i++)
        {
            EpisodeGraphEdgeData edge = Edges[i];

            if (edge == null)
                continue;

            if (!string.Equals(edge.FromEpisodeId, currentEpisodeId, StringComparison.Ordinal))
                continue;

            EpisodeGraphNodeData toNode = FindNode(edge.ToEpisodeId);

            if (toNode != null && toNode.Kind == EpisodeNodeKind.Main)
                return toNode.Id;
        }

        return "";
    }
}