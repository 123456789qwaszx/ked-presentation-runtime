using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChapterSelectionPanelView : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private ChapterButtonCardView[] cards = Array.Empty<ChapterButtonCardView>();

    [Header("Return")]
    [SerializeField] private Button returnButton;

    public event Action<int> ChapterRequested;
    public event Action BackRequested;

    private int _selectedChapterId = -1;

    private void Awake()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            ChapterButtonCardView card = cards[i];
            if (card != null)
                card.Clicked += HandleCardClicked;
        }

        if (returnButton != null)
            returnButton.onClick.AddListener(HandleReturnClicked);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            ChapterButtonCardView card = cards[i];
            if (card != null)
                card.Clicked -= HandleCardClicked;
        }

        if (returnButton != null)
            returnButton.onClick.RemoveListener(HandleReturnClicked);
    }

    public void PresentChapters(
        ChapterButtonCardModel[] models,
        int selectedChapterId = -1)
    {
        _selectedChapterId = selectedChapterId;

        for (int i = 0; i < cards.Length; i++)
        {
            ChapterButtonCardView card = cards[i];
            if (card == null)
                continue;

            if (models != null && i < models.Length)
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

        for (int i = 0; i < cards.Length; i++)
        {
            ChapterButtonCardView card = cards[i];
            if (card == null)
                continue;

            card.SetSelected(card.ChapterId == chapterId);
        }
    }

    private void HandleCardClicked(int chapterId)
    {
        SetSelectedChapter(chapterId);
        ChapterRequested?.Invoke(chapterId);
    }

    private void HandleReturnClicked()
    {
        BackRequested?.Invoke();
    }
}