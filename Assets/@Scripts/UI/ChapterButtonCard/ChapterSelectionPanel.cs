using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ChapterSelectionPanel : UIPanel<ChapterSelectionPanel.Refs>
{
    public event Action CloseClicked;
    
    private Action<ChapterButtonCard> _onChapterCardPressed;
    private Action<ChapterButtonCard> _onChapterCardReleased;
    private Action<ChapterButtonCard> _onChapterCardClicked;
    
    public enum Refs
    {
        SafeArea,
        SelectChapterBG_Root,
        SelectChapterBG_Image,

        ButtonViewport,
        ChapterButtons,

        ReturnBlock_Root,
        CurrentScreenLabel_Root,
        CurrentScreenLabelBG_Image,
        CurrentScreenLabel_Text,
        CurrentScreenLabelIcon_Image,

        ReturnButton_Root,
        ReturnButton,
        ReturnButton_Image,

        CharBlock_Root,
        CharGradient_Root,
        CharGradient_Image,

        Character_Root,
        Character_Image,

        CharHUD_Root,
        CharHUDGradient_Root,
        CharHUDGradient_Image,

        AffinityBadge_Root,
        AffinityBadge_Image,

        CharName_Root,
        CharName_Text,

        ChangePortraitButton_Root,
        ChangePortraitButton,
        ChangePortraitButton_Image,
        ChangePortraitButton_Text,
    }

    private readonly List<ChapterButtonCard> _cards = new();
    
    public RectTransform CardContainer => View.Rect(Refs.ChapterButtons);

    protected override void OnInitialize()
    {
        BindEvent(View.Button(Refs.ReturnButton), HandleOnBackRequested);
    }

    public void SetChapterCardHandlers(
        Action<ChapterButtonCard> onPressed,
        Action<ChapterButtonCard> onReleased,
        Action<ChapterButtonCard> onClicked)
    {
        _onChapterCardPressed = onPressed;
        _onChapterCardReleased = onReleased;
        _onChapterCardClicked = onClicked;

        ApplyCardHandlers();
    }

    public void RegisterCards(IReadOnlyList<ChapterButtonCard> cards)
    {
        ClearCards();

        if (cards == null)
            return;

        ScrollRect scrollRect = FindChapterScrollRect();

        for (int i = 0; i < cards.Count; i++)
        {
            ChapterButtonCard card = cards[i];

            if (card == null)
                continue;

            _cards.Add(card);
            card.SetDragScrollRect(scrollRect);
        }

        ApplyCardHandlers();
    }

    public void PresentChapters(ChapterButtonCardModel[] models)
    {
        int modelCount = models != null ? models.Length : 0;

        for (int i = 0; i < _cards.Count; i++)
        {
            ChapterButtonCard card = _cards[i];

            if (card == null)
                continue;

            bool hasModel = i < modelCount;

            card.gameObject.SetActive(hasModel);

            if (!hasModel)
                continue;

            ChapterButtonCardModel model = models[i];
            card.Present(model);
        }
    }

    private void ApplyCardHandlers()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            ChapterButtonCard card = _cards[i];

            if (card == null)
                continue;

            card.SetHandlers(
                _onChapterCardPressed,
                _onChapterCardReleased,
                _onChapterCardClicked);
        }
    }

    private ScrollRect FindChapterScrollRect()
    {
        RectTransform viewport = View.Rect(Refs.ButtonViewport);

        if (viewport == null)
            return null;

        ScrollRect scrollRect = viewport.GetComponent<ScrollRect>();

        if (scrollRect != null)
            return scrollRect;

        return viewport.GetComponentInParent<ScrollRect>();
    }

    private void ClearCards()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            ChapterButtonCard card = _cards[i];

            if (card == null)
                continue;

            card.ClearHandlers();
        }

        _cards.Clear();
    }

    private void HandleOnBackRequested(PointerEventData _)
    {
        CloseClicked?.Invoke();
    }
}