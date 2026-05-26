using System.Collections.Generic;
using System;

public sealed class EpisodeGraphData
{
    public List<EpisodeGraphNodeData> Nodes = new();
    public List<EpisodeGraphEdgeData> Edges = new();

    public bool ContainsEpisode(string episodeId)
    {
        return FindNode(episodeId) != null;
    }

    public EpisodeGraphNodeData FindNode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return null;

        for (int i = 0; i < Nodes.Count; i++)
        {
            EpisodeGraphNodeData node = Nodes[i];

            if (node != null && string.Equals(node.EpisodeId, episodeId, StringComparison.Ordinal))
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
                return node.EpisodeId;
        }

        if (Nodes.Count > 0 && Nodes[0] != null)
            return Nodes[0].EpisodeId;

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
                return toNode.EpisodeId;
        }

        return "";
    }
}

public sealed class EpisodeGraphNodeData
{
    public string EpisodeId;
    public EpisodeNodeKind Kind;
    public string LayoutParentEpisodeId;
}

public sealed class EpisodeGraphEdgeData
{
    public string FromEpisodeId;
    public string ToEpisodeId;
    public bool IsAttachment;
}

public enum EpisodeNodeKind
{
    Main,
    Attachment
}