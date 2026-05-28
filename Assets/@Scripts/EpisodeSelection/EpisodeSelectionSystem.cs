using System;
using System.Collections.Generic;

public sealed class EpisodeSelectionSystem
{
    private readonly ChapterEpisodeProgressionCatalogSO _progressionCatalog;
    private readonly EpisodeGraphLayoutOptions _layoutOptions;

    private readonly EpisodeYarnEntryMapBuilder _yarnEntryMapBuilder;
    private readonly EpisodeProgressionGraphDataBuilder _graphDataBuilder;
    private readonly EpisodeProgressionRuleDataBuilder _ruleDataBuilder;

    private readonly EpisodeGraphRenderer _episodeGraphRenderer;

    private EpisodeConditionEvaluator _conditionEvaluator;
    private EpisodeGraphViewModelBuilder _viewModelBuilder;

    public EpisodeSelectionStateData _selectionState;

    public EpisodeSelectionSystem(
        ChapterEpisodeProgressionCatalogSO progressionCatalog,
        EpisodeGraphLayoutOptions layoutOptions,
        EpisodeYarnEntryMapBuilder yarnEntryMapBuilder,
        EpisodeProgressionGraphDataBuilder graphDataBuilder,
        EpisodeProgressionRuleDataBuilder ruleDataBuilder,
        EpisodeSelectionStateData selectionState,
        EpisodeGraphRenderer episodeGraphRenderer)
    {
        _progressionCatalog = progressionCatalog;
        _layoutOptions = layoutOptions;

        _yarnEntryMapBuilder = yarnEntryMapBuilder;
        _graphDataBuilder = graphDataBuilder;
        _ruleDataBuilder = ruleDataBuilder;

        _selectionState = selectionState ?? new EpisodeSelectionStateData();
        _episodeGraphRenderer = episodeGraphRenderer;
        
        

        SetChapterId(1);
    }

    public Dictionary<string, EpisodeYarnEntryData> YarnEntryByEpisodeId { get; private set; }
    public EpisodeGraphData CurrentGraphData { get; private set; }
    public EpisodeProgressionRuleData ProgressionRules { get; private set; }

    public string CurrentStartEpisodeId => _selectionState.CurrentStartEpisodeId ?? string.Empty;
    public string SelectedEpisodeId => _selectionState.SelectedEpisodeId ?? string.Empty;

    public string GetYarnNodeName()
    {
        const string fallback = "yarnNodeFallback";

        if (string.IsNullOrEmpty(SelectedEpisodeId))
            return fallback;

        if (!YarnEntryByEpisodeId.TryGetValue(SelectedEpisodeId, out EpisodeYarnEntryData entry))
            return fallback;

        if (string.IsNullOrEmpty(entry.YarnNodeName))
            return fallback;

        return entry.YarnNodeName;
    }

    public void SetEpisodeSelectedHandler(Action<string> handler)
    {
        if (_episodeGraphRenderer == null)
            return;

        _episodeGraphRenderer.SetHandlers(handler);
    }

    public bool SetChapterId(int chapterId)
    {
        if (_progressionCatalog == null)
            return false;

        if (!_progressionCatalog.TryGetProgression(chapterId, out ChapterEpisodeProgressionSO progression))
            return false;

        ApplyChapterProgression(chapterId, progression);
        return true;
    }

    private void ApplyChapterProgression(
        int chapterId,
        ChapterEpisodeProgressionSO progression)
    {
        string chapterIdText = progression.ChapterId ?? chapterId.ToString();
        string displayName = progression.DisplayName ?? string.Empty;
        string startEpisodeId = progression.StartEpisodeId ?? string.Empty;

        _selectionState.CurrentChapterId = chapterIdText;
        _selectionState.CurrentChapterDisplayName = displayName;
        _selectionState.CurrentStartEpisodeId = startEpisodeId;
    }

    public bool DrawEpisodeNodes()
    {
        if (_conditionEvaluator == null || _viewModelBuilder == null)
            return false;

        _selectionState.ResetForChapter(CurrentStartEpisodeId);
        _conditionEvaluator.RebuildAvailabilityState();

        RefreshGraphView();
        return true;
    }

    public void MarkEpisodeCompleted(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        _selectionState.MarkEpisodeCleared(episodeId);

        if (_conditionEvaluator != null)
            _conditionEvaluator.RebuildAvailabilityState();

        RefreshGraphView();
    }

    public bool TrySetSelectedEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return false;

        if (_selectionState.IsEpisodeLocked(episodeId))
            return false;

        _selectionState.SetSelectedEpisodeId(episodeId);
        RefreshGraphView();

        return true;
    }

    private void RefreshGraphView()
    {
        if (_viewModelBuilder == null || _episodeGraphRenderer == null)
            return;

        EpisodeGraphViewData viewData = _viewModelBuilder.Build();
        _episodeGraphRenderer.Render(viewData);
    }
}