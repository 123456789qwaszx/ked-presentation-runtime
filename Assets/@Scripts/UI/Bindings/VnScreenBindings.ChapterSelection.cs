using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VnScreenBindings
{
    private Func<ChapterButtonCardModel[]> _resolveChapterModels;
    private Func<int> _resolveSelectedChapterId;
    private Action<int> _onChapterRequested;

    private readonly ChapterCardRuntimeSpawner _chapterCardSpawner = new ChapterCardRuntimeSpawner();

    private RectTransform _chapterCardPrefab;
    private int _chapterCardCount = 6;
    private ChapterButtonCardBuildOptions _chapterCardBuildOptions;

    public void ConfigureChapterSelection(
        Func<ChapterButtonCardModel[]> resolveChapterModels = null,
        Func<int> resolveSelectedChapterId = null,
        RectTransform chapterCardPrefab = null,
        int chapterCardCount = 6,
        ChapterButtonCardBuildOptions chapterCardBuildOptions = null,
        Action<int> onChapterRequested = null)
    {
        _resolveChapterModels = resolveChapterModels;
        _resolveSelectedChapterId = resolveSelectedChapterId;
        _onChapterRequested = onChapterRequested;

        _chapterCardPrefab = chapterCardPrefab;
        _chapterCardCount = Mathf.Max(0, chapterCardCount);
        _chapterCardBuildOptions = chapterCardBuildOptions;
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

        BuildChapterCards(panel);
        RefreshChapterSelectionPanel(panel);
    }

    private void BuildChapterCards(ChapterSelectionPanel panel)
    {
        if (panel == null)
            return;

        RectTransform container = panel.CardContainer;

        if (container == null)
        {
            Debug.LogWarning("[VnScreenBindings] Chapter card container is null.");
            return;
        }

        ClearChildren(container);

        List<ChapterButtonCard> cards = _chapterCardSpawner.CreateCards(
            container,
            _chapterCardPrefab,
            _chapterCardCount,
            _chapterCardBuildOptions);

        panel.RegisterCards(cards);
    }

    private void ClearChildren(RectTransform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            child.SetParent(null, false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
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
    {
        _onChapterRequested?.Invoke(chapterId);
    }

    private void OnChapterBackRequested()
    {
        GoToLobby();
    }
}