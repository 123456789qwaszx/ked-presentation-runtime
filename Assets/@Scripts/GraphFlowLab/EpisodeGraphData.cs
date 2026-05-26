using System.Collections.Generic;

public sealed class EpisodeGraphData
{
    public List<EpisodeGraphNodeData> Nodes = new();
    public List<EpisodeGraphEdgeData> Edges = new();
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