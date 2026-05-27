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
            BindPanel(panel, ApplyBindings);
            BuildChapterCards(panel);
            Refresh(panel);
        });
    }

    private void ApplyBindings(ChapterSelectionPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += CloseTopPanel,
            p => p.CloseClicked -= CloseTopPanel);

        panel.SetChapterCardHandlers(
            onPressed: OnChapterCardPressed,
            onReleased: OnChapterCardReleased,
            onClicked: OnChapterCardClicked);
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
    
    private void BuildChapterCards(ChapterSelectionPanel panel)
    {
        RectTransform container = panel.CardContainer;
        ClearChildren(container);

        List<ChapterButtonCard> cards = _chapterCardSpawner.CreateCards(container, _chapterCardPrefab, _chapterCardCount);

        panel.RegisterCards(cards);
    }

    private void Refresh(ChapterSelectionPanel panel)
    {
        ChapterButtonCardModel[] models = null;

        if (_resolveChapterModels != null)
            models = _resolveChapterModels.Invoke();

        panel.PresentChapters(models);
    }
    
    private void ClearChildren(RectTransform parent)
    {
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