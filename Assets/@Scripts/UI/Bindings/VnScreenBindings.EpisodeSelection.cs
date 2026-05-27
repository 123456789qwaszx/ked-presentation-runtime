public sealed partial class VnScreenBindings
{
    private EpisodeSelectionController _episodeSelectionController;
    
    private int _currentChapterId = -1;

    public void ConfigureEpisodeSelection(EpisodeSelectionController episodeSelectionController)
    {
        _episodeSelectionController = episodeSelectionController;
    }

    private void OpenEpisodeSelectionPanel(int chapterId)
    {
        _currentChapterId = chapterId;

        UI.PushPanel<EpisodeSelectionPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);

            Refresh(panel);

            _episodeSelectionController.RequestRender();
        });
    }

    private void ApplyBindings(EpisodeSelectionPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);

        _episodeSelectionController.EpisodeRequested += OnEpisodeRequested;

        AddCleanup(panel, () =>
        {
            _episodeSelectionController.EpisodeRequested -= OnEpisodeRequested;
        });
    }

    private void OnEpisodeRequested(string episodeId)
    {
        if (!_episodeSelectionController.TryGetDialogueEntryId(episodeId, out string dialogueEntryId))
            return;
        
        CloseAllPanels();

        _episodePlayer.StartGame(dialogueEntryId);
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