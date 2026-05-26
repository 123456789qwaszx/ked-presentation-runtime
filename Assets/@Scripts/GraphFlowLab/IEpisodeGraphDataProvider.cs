public interface IEpisodeGraphDataProvider
{
    EpisodeGraphData GetGraphData();
}

public sealed class SampleEpisodeGraphDataProvider : IEpisodeGraphDataProvider
{
    public EpisodeGraphData GetGraphData()
    {
        EpisodeGraphData data = new EpisodeGraphData();

        data.Nodes.Add(new EpisodeGraphNodeData
        {
            EpisodeId = "main05.01",
            Kind = EpisodeNodeKind.Main,
            LayoutParentEpisodeId = ""
        });

        data.Nodes.Add(new EpisodeGraphNodeData
        {
            EpisodeId = "main05.02",
            Kind = EpisodeNodeKind.Main,
            LayoutParentEpisodeId = ""
        });

        data.Nodes.Add(new EpisodeGraphNodeData
        {
            EpisodeId = "main05.03",
            Kind = EpisodeNodeKind.Main,
            LayoutParentEpisodeId = ""
        });

        data.Nodes.Add(new EpisodeGraphNodeData
        {
            EpisodeId = "upper05.02",
            Kind = EpisodeNodeKind.Attachment,
            LayoutParentEpisodeId = "main05.02"
        });

        data.Nodes.Add(new EpisodeGraphNodeData
        {
            EpisodeId = "lower05.02",
            Kind = EpisodeNodeKind.Attachment,
            LayoutParentEpisodeId = "main05.02"
        });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.01",
            ToEpisodeId = "main05.02",
            IsAttachment = false
        });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.02",
            ToEpisodeId = "main05.03",
            IsAttachment = false
        });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.02",
            ToEpisodeId = "upper05.02",
            IsAttachment = true
        });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.02",
            ToEpisodeId = "lower05.02",
            IsAttachment = true
        });

        return data;
    }
}