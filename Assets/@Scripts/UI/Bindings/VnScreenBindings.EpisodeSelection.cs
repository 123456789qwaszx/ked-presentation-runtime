using UnityEngine;

public sealed partial class VnScreenBindings
{
    private EpisodeSelectionSystem _episodeSelectionSystem;
    
    private int _currentChapterId = -1;

    public void ConfigureEpisodeSelection(EpisodeSelectionSystem episodeSelectionSystem)
    {
        _episodeSelectionSystem = episodeSelectionSystem;
    }

    private void OpenEpisodeSelectionPanel(int chapterId)
    {
        _currentChapterId = chapterId;

        UI.PushPanel<EpisodeSelectionPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);

            Refresh(panel);

            _episodeSelectionSystem.DrawEpisodeNodes(chapterId);
        });
    }

    private void ApplyBindings(EpisodeSelectionPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);

        if (_episodeSelectionSystem == null)
            return;

        EpisodeSelectionSystem system = _episodeSelectionSystem;

        system.EpisodeRequested += OnEpisodeRequested;

        AddCleanup(panel, () =>
        {
            system.EpisodeRequested -= OnEpisodeRequested;
        });
    }

    private void OnEpisodeRequested(string episodeId)
    {
        Debug.Log(episodeId);

        if (_episodeSelectionSystem == null)
            return;

        if (!_episodeSelectionSystem.TryGetDialogueEntryId(
                episodeId,
                out string dialogueEntryId))
        {
            return;
        }
        
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