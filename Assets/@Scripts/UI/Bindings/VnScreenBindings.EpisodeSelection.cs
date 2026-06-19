public sealed partial class VnScreenBindings
{
    private EpisodeSelectionSystem _episodeSelectionSystem;

    public void ConfigureEpisodeSelection(EpisodeSelectionSystem episodeSelectionSystem)
    {
        _episodeSelectionSystem = episodeSelectionSystem;
        _episodeSelectionSystem.SetEpisodeSelectedHandler(HandleEpisodeSelected);
    }
    
    #region EpisodeNode
    
    private void HandleEpisodeSelected(string episodeId)
    {
        if (!_episodeSelectionSystem.MarkEpisodeSelected(episodeId))
            return;
        
        OpenEpisodeConfirmPanel();
    }
    #endregion
    
    #region EpisodeSelectionPanel
    
    private void OpenEpisodeSelectionPanel()
    {
        UI.PushPanel<EpisodeSelectionPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            Refresh(panel);
        });
    }

    private void ApplyBindings(EpisodeSelectionPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);
    }
    
    private void Refresh(EpisodeSelectionPanel panel)
    {
        ChapterMetaModel model = CreateDebugEpisodeSelectionPanelModel();
        PlayerStateSnapshot state = CreateDebugPlayerStateSnapshot();

        panel.Present(model, state);
    }
    #endregion

    private ChapterMetaModel CreateDebugEpisodeSelectionPanelModel()
    {
        return new ChapterMetaModel(
            chapterIndex: $"CHAPTER 00",
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