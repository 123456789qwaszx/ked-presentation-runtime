using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ChapterSelectionPanel : UIPanel<ChapterSelectionPanel.Refs>
{
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

    public event Action<int> OnChapterRequested;
    public event Action OnBackRequested;

    private readonly List<ChapterButtonCard> _cards = new();

    private int _selectedChapterId = -1;

    public RectTransform CardContainer => View.Rect(Refs.ChapterButtons);

    protected override void OnInitialize()
    {
        BindEvent(View.Button(Refs.ReturnButton), OnReturn);
    }

    public void RegisterCards(IReadOnlyList<ChapterButtonCard> cards)
    {
        UnregisterCards();

        if (cards == null)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            ChapterButtonCard card = cards[i];

            if (card == null)
                continue;

            card.Clicked += HandleCardClicked;
            _cards.Add(card);
        }
    }

    public void UnregisterCards()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            ChapterButtonCard card = _cards[i];

            if (card == null)
                continue;

            card.Clicked -= HandleCardClicked;
        }

        _cards.Clear();
    }

    public void PresentChapters(ChapterButtonCardModel[] models, int selectedChapterId = -1)
    {
        _selectedChapterId = selectedChapterId;

        for (int i = 0; i < _cards.Count; i++)
        {
            ChapterButtonCard card = _cards[i];

            if (card == null)
                continue;

            bool hasModel = models != null && i < models.Length;

            if (hasModel)
            {
                ChapterButtonCardModel model = models[i];

                card.gameObject.SetActive(true);
                card.Present(model);
                card.SetSelected(model.ChapterId == _selectedChapterId);
            }
            else
            {
                card.Present(ChapterButtonCardModel.Empty());
                card.SetSelected(false);
                card.gameObject.SetActive(false);
            }
        }
    }

    public void SetSelectedChapter(int chapterId)
    {
        _selectedChapterId = chapterId;

        for (int i = 0; i < _cards.Count; i++)
        {
            ChapterButtonCard card = _cards[i];

            if (card == null)
                continue;

            card.SetSelected(card.ChapterId == chapterId);
        }
    }

    private void HandleCardClicked(int chapterId)
    {
        SetSelectedChapter(chapterId);
        OnChapterRequested?.Invoke(chapterId);
    }

    private void OnReturn(PointerEventData _)
    {
        OnBackRequested?.Invoke();
    }
}