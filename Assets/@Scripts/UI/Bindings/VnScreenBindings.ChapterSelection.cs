using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VnScreenBindings
{
    private Func<ChapterButtonCardModel[]> _resolveChapterModels;

    private readonly ChapterCardRuntimeSpawner _chapterCardSpawner = new ();

    private RectTransform _chapterCardPrefab;
    private int _chapterCardCount = 6;

    public void ConfigureChapterSelection(
        Func<ChapterButtonCardModel[]> resolveChapterModels = null,
        RectTransform chapterCardPrefab = null,
        int chapterCardCount = 6)
    {
        _resolveChapterModels = resolveChapterModels;

        _chapterCardPrefab = chapterCardPrefab;
        _chapterCardCount = Mathf.Max(0, chapterCardCount);
    }

    public void GoToChapterSelection()
    {
        UI.PushPanel<ChapterSelectionPanel>(panel =>
        {
            BindRoot(panel, BindChapterSelectionPanel);
            
            BuildChapterCards(panel);
            RefreshChapterSelectionPanel(panel);
        });
    }

    private void BindChapterSelectionPanel(ChapterSelectionPanel panel)
    {
        if (panel == null)
            return;

        _ctx.Bind(panel,
            p => p.OnBackRequested += OnChapterBackRequested,
            p => p.OnBackRequested -= OnChapterBackRequested);

        panel.SetChapterCardHandlers(
            onPressed: OnChapterCardPressed,
            onReleased: OnChapterCardReleased,
            onClicked: OnChapterCardClicked);

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

        List<ChapterButtonCard> cards = _chapterCardSpawner.CreateCards(container, _chapterCardPrefab, _chapterCardCount);

        panel.RegisterCards(cards);
    }

    private void RefreshChapterSelectionPanel(ChapterSelectionPanel panel)
    {
        if (panel == null)
            return;

        ChapterButtonCardModel[] models = null;

        if (_resolveChapterModels != null)
            models = _resolveChapterModels.Invoke();

        panel.PresentChapters(models);
    }

    private void OnChapterCardPressed(ChapterButtonCard card)
    {
        if (card == null)
            return;
    }

    private void OnChapterCardReleased(ChapterButtonCard card)
    {
        if (card == null)
            return;
    }

    private void OnChapterCardClicked(ChapterButtonCard card)
    {
        if (card == null)
            return;

        int chapterId = card.ChapterId;

        if (chapterId < 0)
            return;

        GoToEpisodeSelection(chapterId);
    }

    private void OnChapterBackRequested()
    {
        GoToLobby();
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
}