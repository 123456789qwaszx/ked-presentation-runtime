using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChapterButtonCardView : MonoBehaviour
{
    [Header("Hit")]
    [SerializeField] private Button hitButton;

    [Header("Text")]
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private TMP_Text chapterIndexLabelText;
    [SerializeField] private TMP_Text chapterTitleText;
    [SerializeField] private TMP_Text episodeHeadingText;

    [Header("Images")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image bgOverlayImage;
    [SerializeField] private Image chapterIndexLabelImage;
    [SerializeField] private Image episodeHeadingLabelImage;
    [SerializeField] private Image titleIconImage;

    [Header("State")]
    [SerializeField] private CanvasGroup selectedRoot;
    [SerializeField] private CanvasGroup lockRoot;

    public event Action<int> Clicked;

    public int ChapterId { get; private set; } = -1;

    private void Awake()
    {
        if (hitButton != null)
            hitButton.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (hitButton != null)
            hitButton.onClick.RemoveListener(HandleClick);
    }

    public void Present(in ChapterButtonCardModel model)
    {
        ChapterId = model.ChapterId;

        SetText(indexText, model.IndexText);
        SetText(chapterIndexLabelText, model.ChapterIndexLabel);
        SetText(chapterTitleText, model.ChapterTitle);
        SetText(episodeHeadingText, model.EpisodeHeading);

        SetSpriteIfNotNull(bgImage, model.Bg);
        SetSpriteIfNotNull(bgOverlayImage, model.BgOverlay);
        SetSpriteIfNotNull(chapterIndexLabelImage, model.ChapterIndexLabelSprite);
        SetSpriteIfNotNull(episodeHeadingLabelImage, model.EpisodeHeadingLabelSprite);
        SetSpriteIfNotNull(titleIconImage, model.TitleIcon);

        SetInteractable(model.Interactable && !model.Locked);
        SetLocked(model.Locked);
    }

    public void SetSelected(bool selected)
    {
        SetVisible(selectedRoot, selected, false);
    }

    public void SetLocked(bool locked)
    {
        SetVisible(lockRoot, locked, locked);
    }

    private void SetInteractable(bool interactable)
    {
        if (hitButton != null)
            hitButton.interactable = interactable;
    }

    private void HandleClick()
    {
        if (ChapterId < 0)
            return;

        Clicked?.Invoke(ChapterId);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value ?? "";
    }

    private static void SetSpriteIfNotNull(Image target, Sprite sprite)
    {
        if (target != null && sprite != null)
            target.sprite = sprite;
    }

    private static void SetVisible(
        CanvasGroup group,
        bool visible,
        bool blockRaycasts)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = blockRaycasts;
    }
}