using System.Collections.Generic;
using UnityEngine;

public sealed partial class VnScreenBindings
{
    private readonly ChapterCardRuntimeSpawner _chapterCardSpawner = new();

    private ChapterCardFactory _chapterCardFactory;
    private RectTransform _chapterCardPrefab;

    public void ConfigureChapterSelection(
        ChapterCardFactory chapterCardFactory,
        RectTransform chapterCardPrefab = null)
    {
        _chapterCardFactory = chapterCardFactory;
        _chapterCardPrefab = chapterCardPrefab;
    }

    private void OpenChapterSelectionPanel()
    {
        UI.PushPanel<ChapterSelectionPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);

            ChapterButtonCardModel[] models = _chapterCardFactory.CreateModels();
            
            List<ChapterButtonCard> cards = _chapterCardSpawner.CreateCards(_chapterCardPrefab, models.Length);
            panel.RegisterCards(cards);
            
            panel.PresentChapters(models);
        });
    }

    private void ApplyBindings(ChapterSelectionPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);

        AddBinding(panel,
            p => p.SetChapterCardHandlers(
                onPressed: OnChapterCardPressed,
                onReleased: OnChapterCardReleased,
                onClicked: OnChapterCardClicked),
            p => p.SetChapterCardHandlers(
                onPressed: null,
                onReleased: null,
                onClicked: null));
    }

    private void OnChapterCardPressed(ChapterButtonCard card)
    {
    }

    private void OnChapterCardReleased(ChapterButtonCard card)
    {
    }

    private void OnChapterCardClicked(ChapterButtonCard card)
    {
        Debug.Log(card.ChapterId);
        OpenEpisodeSelectionPanel(card.ChapterId);
    }
}