using UnityEngine;

public sealed partial class VnScreenBindings
{
    private EpisodeSelectionController _episodeSelectionController;
    
    private int _currentChapterId = -1;

    public void ConfigureEpisodeSelection(EpisodeSelectionController episodeSelectionController)
    {
        _episodeSelectionController = episodeSelectionController;
    }

    public void GoToEpisodeSelection(int chapterId)
    {
        _currentChapterId = chapterId;

        UI.PushPanel<EpisodeSelectionPanel>(panel => { BindRoot(panel, BindEpisodeSelectionPanel); });
        
        _episodeSelectionController.RequestRender();
    }

    private void BindEpisodeSelectionPanel(EpisodeSelectionPanel panel)
    {
        if (panel == null)
            return;

        _ctx.Bind(panel,
            p => p.OnBackRequested += OnEpisodeSelectionBackRequested,
            p => p.OnBackRequested -= OnEpisodeSelectionBackRequested);

        RefreshEpisodeSelectionPanel(panel);
    }

    private void RefreshEpisodeSelectionPanel(EpisodeSelectionPanel panel)
    {
        if (panel == null)
            return;

        ChapterMetaModel model = CreateDebugEpisodeSelectionPanelModel(_currentChapterId);

        PlayerStateSnapshot state = CreateDebugPlayerStateSnapshot();

        panel.Present(model, state);
    }

    private void OnEpisodeMainRequested(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        Debug.Log($"[VnScreenBindings] Episode main clicked: {episodeId}");
        _episodeSelectionController.RequestSelectEpisode(episodeId);
    }

    private void OnEpisodeSelectionBackRequested()
    {
        GoToChapterSelection();
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