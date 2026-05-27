public sealed partial class VnScreenBindings
{
    private EpisodeSelectionController _episodeSelectionController;
    
    private int _currentChapterId = -1;

    public void ConfigureEpisodeSelection(EpisodeSelectionController episodeSelectionController)
    {
        _episodeSelectionController = episodeSelectionController;
    }

    private void GoToEpisodeSelection(int chapterId)
    {
        _currentChapterId = chapterId;

        UI.PushPanel<EpisodeSelectionPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            _episodeSelectionController.RequestRender();
            Refresh(panel);
        });
    }

    private void ApplyBindings(EpisodeSelectionPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += CloseTopPanel,
            p => p.CloseClicked -= CloseTopPanel);
    }

    private void Refresh(EpisodeSelectionPanel panel)
    {
        ChapterMetaModel model = CreateDebugEpisodeSelectionPanelModel(_currentChapterId);
        PlayerStateSnapshot state = CreateDebugPlayerStateSnapshot();

        panel.Present(model, state);
    }

    private ChapterMetaModel CreateDebugEpisodeSelectionPanelModel(int chapterId)
    {
        return new ChapterMetaModel(
            chapterIndex: $"CHAPTER {chapterId:00}",
            eraText: "STELLA ERA",
            chapterTitle: "테스트 에피소드 그래프");
    }

    private PlayerStateSnapshot CreateDebugPlayerStateSnapshot()
    {
        return new PlayerStateSnapshot(
            intuition: 30,
            analysis: 35,
            chaos: 30);
    }
}