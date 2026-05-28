using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeYarnEntryData
{
    public string EpisodeId;
    public EpisodeNodeKind Kind;
    public string YarnNodeName;
}

public sealed class EpisodeSelectionRuntimeContext
{
    private readonly ChapterEpisodeProgressionCatalogSO _progressionCatalog;

    private readonly EpisodeYarnEntryMapBuilder _yarnEntryMapBuilder;
    private readonly EpisodeProgressionGraphDataBuilder _graphDataBuilder;
    private readonly EpisodeProgressionRuleDataBuilder _ruleDataBuilder;

    public string CurrentChapterId { get; private set; } = "";
    public string CurrentChapterDisplayName { get; private set; } = "";
    public string CurrentStartEpisodeId { get; private set; } = "";

    public Dictionary<string, EpisodeYarnEntryData> YarnEntryByEpisodeId { get; private set; } = new(StringComparer.Ordinal);

    public EpisodeGraphData CurrentGraphData { get; private set; }
    public EpisodeProgressionRuleData ProgressionRules { get; private set; }
    public EpisodeSelectionStateData State { get; private set; } = new();

    public EpisodeSelectionRuntimeContext(
        ChapterEpisodeProgressionCatalogSO progressionCatalog,
        EpisodeYarnEntryMapBuilder yarnEntryMapBuilder,
        EpisodeProgressionGraphDataBuilder graphDataBuilder,
        EpisodeProgressionRuleDataBuilder ruleDataBuilder)
    {
        _progressionCatalog = progressionCatalog;
        _yarnEntryMapBuilder = yarnEntryMapBuilder;
        _graphDataBuilder = graphDataBuilder;
        _ruleDataBuilder = ruleDataBuilder;
    }

    public bool OpenChapter(int chapterId)
    {
        if (!_progressionCatalog.TryGetProgression(chapterId, out ChapterEpisodeProgressionSO progression))
            return false;

        CurrentChapterId = progression.ChapterId ?? "";
        CurrentChapterDisplayName = progression.DisplayName ?? "";
        CurrentStartEpisodeId = progression.StartEpisodeId ?? "";

        YarnEntryByEpisodeId = _yarnEntryMapBuilder.Build(progression);
        CurrentGraphData = _graphDataBuilder.Build(progression);
        ProgressionRules = _ruleDataBuilder.Build(progression);

        State.ResetForChapter(CurrentStartEpisodeId);
        return true;
    }

    public EpisodeGraphNodeData GetNode(string episodeId)
    {
        return CurrentGraphData.FindNode(episodeId);
    }

    public EpisodeNodeRuleData GetNodeRule(string episodeId)
    {
        return ProgressionRules.GetNodeRule(episodeId);
    }

    public EpisodeSelectionRuntimeContext CloneRuntimeValuesOnly()
    {
        EpisodeSelectionRuntimeContext clone = new EpisodeSelectionRuntimeContext(
            _progressionCatalog,
            _yarnEntryMapBuilder,
            _graphDataBuilder,
            _ruleDataBuilder)
        {
            CurrentChapterId = CurrentChapterId,
            CurrentChapterDisplayName = CurrentChapterDisplayName,
            CurrentStartEpisodeId = CurrentStartEpisodeId,
            YarnEntryByEpisodeId = new Dictionary<string, EpisodeYarnEntryData>(
                YarnEntryByEpisodeId,
                StringComparer.Ordinal),
            CurrentGraphData = CurrentGraphData,
            ProgressionRules = ProgressionRules,
            State = State.Clone()
        };

        return clone;
    }
}