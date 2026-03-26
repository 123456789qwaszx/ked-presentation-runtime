public readonly struct EpisodeSelectionPanelModel
{
    public readonly int ChapterId;
    public readonly ChapterMetaModel ChapterMeta;

    public readonly EpisodeGraphModel Graph;

    public readonly string SelectedEpisodeId;

    public EpisodeSelectionPanelModel(
        int chapterId,
        ChapterMetaModel chapterMeta,
        EpisodeGraphModel graph,
        string selectedEpisodeId = null)
    {
        ChapterId = chapterId;
        ChapterMeta = chapterMeta;
        Graph = graph;
        SelectedEpisodeId = selectedEpisodeId;
    }
}

public readonly struct ChapterMetaModel
{
    public readonly string ChapterIndex; // "챕터 5"
    public readonly string EraText;      // "성력 996년"
    public readonly string ChapterTitle; // "짙은 밤에 드리운 불빛"

    public ChapterMetaModel(string chapterIndex, string eraText, string chapterTitle)
    {
        ChapterIndex = chapterIndex;
        EraText = eraText;
        ChapterTitle = chapterTitle;
    }
}