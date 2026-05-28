using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeYarnEntryData
{
    public string EpisodeId;
    public EpisodeNodeKind Kind;
    public string YarnNodeName;
}

[Serializable]
public sealed class EpisodeYarnEntryMapData
{
    private readonly Dictionary<int, Dictionary<string, EpisodeYarnEntryData>> _entryByChapterId
        = new Dictionary<int, Dictionary<string, EpisodeYarnEntryData>>();

    public void AddChapterEntries(
        int chapterId,
        Dictionary<string, EpisodeYarnEntryData> entries)
    {
        if (entries == null)
            entries = new Dictionary<string, EpisodeYarnEntryData>(StringComparer.Ordinal);

        _entryByChapterId[chapterId] = entries;
    }

    public Dictionary<string, EpisodeYarnEntryData> GetChapterEntries(int chapterId)
    {
        if (_entryByChapterId.TryGetValue(chapterId, out Dictionary<string, EpisodeYarnEntryData> entries))
            return entries;

        return new Dictionary<string, EpisodeYarnEntryData>(StringComparer.Ordinal);
    }
}


[Serializable]
public sealed class EpisodeGraphCatalogData
{
    private readonly Dictionary<int, EpisodeGraphData> _graphDataByChapterId
        = new Dictionary<int, EpisodeGraphData>();

    public void AddChapterGraphData(
        int chapterId,
        EpisodeGraphData graphData)
    {
        if (graphData == null)
            graphData = new EpisodeGraphData();

        _graphDataByChapterId[chapterId] = graphData;
    }

    public EpisodeGraphData GetChapterGraphData(int chapterId)
    {
        if (_graphDataByChapterId.TryGetValue(chapterId, out EpisodeGraphData graphData))
            return graphData;

        return new EpisodeGraphData();
    }
}


[Serializable]
public sealed class EpisodeProgressionRuleCatalogData
{
    private readonly Dictionary<int, EpisodeProgressionRuleData> _ruleDataByChapterId
        = new Dictionary<int, EpisodeProgressionRuleData>();

    public void AddChapterRuleData(
        int chapterId,
        EpisodeProgressionRuleData ruleData)
    {
        if (ruleData == null)
            ruleData = new EpisodeProgressionRuleData();

        _ruleDataByChapterId[chapterId] = ruleData;
    }

    public EpisodeProgressionRuleData GetChapterRuleData(int chapterId)
    {
        if (_ruleDataByChapterId.TryGetValue(chapterId, out EpisodeProgressionRuleData ruleData))
            return ruleData;

        return new EpisodeProgressionRuleData();
    }
}