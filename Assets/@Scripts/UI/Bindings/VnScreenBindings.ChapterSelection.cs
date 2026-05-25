using System;

public sealed partial class VnScreenBindings
{
    private Func<ChapterButtonCardModel[]> _resolveChapterModels;
    private Func<int> _resolveSelectedChapterId;
    
    public void ConfigureChapterSelection(
        Func<ChapterButtonCardModel[]> resolveChapterModels = null,
        Func<int> resolveSelectedChapterId = null)
    {
        _resolveChapterModels = resolveChapterModels;
        _resolveSelectedChapterId = resolveSelectedChapterId;
    }

    public void GoToChapterSelection()
    {
        UI.PushPanel<ChapterSelectionPanel>(panel =>
        {
            BindRoot(panel, BindChapterSelectionPanel);
        });
    }

    private void BindChapterSelectionPanel(ChapterSelectionPanel panel)
    {
        _ctx.Bind(
            panel,
            p => p.OnChapterRequested += OnChapterRequested,
            p => p.OnChapterRequested -= OnChapterRequested);

        _ctx.Bind(
            panel,
            p => p.OnBackRequested += OnChapterBackRequested,
            p => p.OnBackRequested -= OnChapterBackRequested);

        RefreshChapterSelectionPanel(panel);
    }

    private void RefreshChapterSelectionPanel(ChapterSelectionPanel panel)
    {
        if (panel == null)
            return;

        ChapterButtonCardModel[] models = null;
        int selectedChapterId = -1;

        if (_resolveChapterModels != null)
            models = _resolveChapterModels.Invoke();

        if (_resolveSelectedChapterId != null)
            selectedChapterId = _resolveSelectedChapterId.Invoke();

        panel.PresentChapters(models, selectedChapterId);
    }

    private void OnChapterRequested(int chapterId)
    { }

    private void OnChapterBackRequested()
    {
        GoToLobby();
    }
}