public sealed class EpisodeSelectionRuntimeContext
{
    private readonly ChapterEpisodeProgressionCatalogSO _progressionCatalog;

    private readonly EpisodeChapterRuntimeDataBuilder _chapterDataBuilder;
    private readonly EpisodeProgressionGraphDataBuilder _graphDataBuilder;
    private readonly EpisodeProgressionRuleDataBuilder _ruleDataBuilder;

    public EpisodeChapterRuntimeData CurrentChapter { get; private set; }
    public EpisodeGraphData CurrentGraphData { get; private set; }
    public EpisodeProgressionRuleData ProgressionRules { get; private set; }
    public EpisodeSelectionStateData State { get; private set; } = new();

    public EpisodeSelectionRuntimeContext(
        ChapterEpisodeProgressionCatalogSO progressionCatalog,
        EpisodeChapterRuntimeDataBuilder chapterDataBuilder,
        EpisodeProgressionGraphDataBuilder graphDataBuilder,
        EpisodeProgressionRuleDataBuilder ruleDataBuilder)
    {
        _progressionCatalog = progressionCatalog;
        _chapterDataBuilder = chapterDataBuilder;
        _graphDataBuilder = graphDataBuilder;
        _ruleDataBuilder = ruleDataBuilder;
    }

    public bool OpenChapter(int chapterId)
    {
        if (!_progressionCatalog.TryGetProgression(chapterId, out ChapterEpisodeProgressionSO progression))
            return false;

        CurrentChapter = _chapterDataBuilder.Build(progression);
        CurrentGraphData = _graphDataBuilder.Build(progression);
        ProgressionRules = _ruleDataBuilder.Build(progression);

        State.ResetForChapter(CurrentChapter.StartEpisodeId);
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
            _chapterDataBuilder,
            _graphDataBuilder,
            _ruleDataBuilder)
        {
            CurrentChapter = CurrentChapter,
            CurrentGraphData = CurrentGraphData,
            ProgressionRules = ProgressionRules,
            State = State.Clone()
        };

        return clone;
    }
}