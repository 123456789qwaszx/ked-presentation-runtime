using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ChapterSelectionPanel : UIPanel<ChapterSelectionPanel.Refs>
{
    public event Action<int> OnChapterRequested;
    public event Action OnBackRequested;
    
    public enum Refs
    {
        SafeArea,
        SelectChapterBG_Root,
        SelectChapterBG_Image,

        ButtonViewport,
        ChapterButtons,
        ChapterCard01,
        ChapterCard02,
        ChapterCard03,
        ChapterCard04,
        ChapterCard05,
        ChapterCard06,

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

    private static readonly Refs[] ChapterCardRefs =
    {
        Refs.ChapterCard01,
        Refs.ChapterCard02,
        Refs.ChapterCard03,
        Refs.ChapterCard04,
        Refs.ChapterCard05,
        Refs.ChapterCard06,
    };


    private readonly ChapterButtonCard[] _cards = new ChapterButtonCard[ChapterCardRefs.Length];

    private int _selectedChapterId = -1;

    protected override void OnInitialize()
    {
        for (int i = 0; i < ChapterCardRefs.Length; i++)
        {
            _cards[i] = ResolveCard(ChapterCardRefs[i]);
        }

        for (int i = 0; i < _cards.Length; i++)
        {
            ChapterButtonCard card = _cards[i];

            if (card == null)
                continue;

            card.Clicked += HandleCardClicked;
        }

        BindEvent(View.Button(Refs.ReturnButton), OnReturn);
    }

    private ChapterButtonCard ResolveCard(Refs r)
    {
        RectTransform rect = View.Rect(r);

        if (rect == null)
        {
            Debug.LogWarning($"[ChapterSelectionPanel] Missing {r} ref.", this);
            return null;
        }

        ChapterButtonCard card = rect.GetComponent<ChapterButtonCard>();

        if (card == null)
        {
            Debug.LogWarning($"[ChapterSelectionPanel] {r} has no ChapterButtonCard component.", this);
            return null;
        }

        return card;
    }

    public void PresentChapters(ChapterButtonCardModel[] models, int selectedChapterId = -1)
    {
        _selectedChapterId = selectedChapterId;

        for (int i = 0; i < _cards.Length; i++)
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

        for (int i = 0; i < _cards.Length; i++)
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