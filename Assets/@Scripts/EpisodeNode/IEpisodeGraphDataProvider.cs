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
            Id = "main05.01",
            Kind = EpisodeNodeKind.Main,
        });

        data.Nodes.Add(new EpisodeGraphNodeData
        {
            Id = "main05.02",
            Kind = EpisodeNodeKind.Main,
        });

        data.Nodes.Add(new EpisodeGraphNodeData
        {
            Id = "main05.03",
            Kind = EpisodeNodeKind.Main,
        });

        // data.Nodes.Add(new EpisodeGraphNodeData
        // {
        //     Id = "upper05.02",
        //     Kind = EpisodeNodeKind.Main,
        // });
        //
        // data.Nodes.Add(new EpisodeGraphNodeData
        // {
        //     Id = "lower05.02",
        //     Kind = EpisodeNodeKind.Main,
        // });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.01",
            ToEpisodeId = "main05.02",
        });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.02",
            ToEpisodeId = "main05.03",
        });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.02",
            ToEpisodeId = "upper05.02",
        });

        data.Edges.Add(new EpisodeGraphEdgeData
        {
            FromEpisodeId = "main05.02",
            ToEpisodeId = "lower05.02",
        });

        return data;
    }
}