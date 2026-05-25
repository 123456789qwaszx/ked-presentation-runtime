using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChapterButtonCard : MonoBehaviour
{
    [Serializable]
    public struct References
    {
        [Header("Root / Motion")]
        public RectTransform cardRoot;
        public CanvasGroup cardCanvasGroup;

        public RectTransform layoutRoot;
        public RectTransform motionRoot;
        public RectTransform shakeRoot;
        public RectTransform scaleRoot;

        [Header("Background")]
        public RectTransform bgRoot;
        public RectTransform bgPad;
        public Image bgImage;

        public RectTransform bgOverlayRoot;
        public RectTransform bgOverlayPad;
        public Image bgOverlayImage;

        [Header("Index")]
        public RectTransform indexRoot;
        public RectTransform indexAnchor;
        public TMP_Text indexText;

        [Header("Heading")]
        public RectTransform headingBlockRoot;

        public RectTransform chapterIndexLabelRoot;
        public Image chapterIndexLabelImage;
        public TMP_Text chapterIndexLabelText;

        public RectTransform chapterTitleLabelRoot;
        public Image chapterTitleLabelBgImage;
        public Image chapterTitleLabelIconImage;
        public TMP_Text chapterTitleLabelText;

        public RectTransform episodeHeadingLabelRoot;
        public Image episodeHeadingLabelImage;
        public TMP_Text episodeHeadingLabelText;

        [Header("Input")]
        public RectTransform hitRoot;
        public Button hitButton;

        [Header("State")]
        public RectTransform selectedRoot;
        public CanvasGroup selectedCanvasGroup;

        public RectTransform lockedRoot;
        public CanvasGroup lockedCanvasGroup;

        [Header("Extension")]
        public RectTransform extensionsRoot;

        public bool HasRequired()
        {
            return
                cardRoot != null &&
                cardCanvasGroup != null &&

                layoutRoot != null &&
                motionRoot != null &&
                shakeRoot != null &&
                scaleRoot != null &&

                bgRoot != null &&
                bgPad != null &&
                bgImage != null &&

                bgOverlayRoot != null &&
                bgOverlayPad != null &&
                bgOverlayImage != null &&

                indexRoot != null &&
                indexAnchor != null &&
                indexText != null &&

                headingBlockRoot != null &&

                chapterIndexLabelRoot != null &&
                chapterIndexLabelImage != null &&
                chapterIndexLabelText != null &&

                chapterTitleLabelRoot != null &&
                chapterTitleLabelBgImage != null &&
                chapterTitleLabelIconImage != null &&
                chapterTitleLabelText != null &&

                episodeHeadingLabelRoot != null &&
                episodeHeadingLabelImage != null &&
                episodeHeadingLabelText != null &&

                hitRoot != null &&
                hitButton != null &&

                selectedRoot != null &&
                selectedCanvasGroup != null &&

                lockedRoot != null &&
                lockedCanvasGroup != null &&

                extensionsRoot != null;
        }
    }

    public event Action<int> Clicked;

    [SerializeField] private References refs;

    private Button _boundHitButton;

    public int ChapterId { get; private set; } = -1;

    public RectTransform CardRoot => refs.cardRoot;
    public CanvasGroup CardCanvasGroup => refs.cardCanvasGroup;

    public RectTransform LayoutRoot => refs.layoutRoot;
    public RectTransform MotionRoot => refs.motionRoot;
    public RectTransform ShakeRoot => refs.shakeRoot;
    public RectTransform ScaleRoot => refs.scaleRoot;

    public CanvasGroup SelectedCanvasGroup => refs.selectedCanvasGroup;
    public CanvasGroup LockedCanvasGroup => refs.lockedCanvasGroup;

    public RectTransform ExtensionsRoot => refs.extensionsRoot;

    private void Awake()
    {
        RebindHitButton();
    }

    private void OnDestroy()
    {
        UnbindHitButton();
    }

    public void Present(in ChapterButtonCardModel model)
    {
        ChapterId = model.ChapterId;

        if (refs.indexText != null)
            refs.indexText.text = model.IndexText;

        if (refs.chapterIndexLabelText != null)
            refs.chapterIndexLabelText.text = model.ChapterIndexLabel;

        if (refs.chapterTitleLabelText != null)
            refs.chapterTitleLabelText.text = model.ChapterTitle;

        if (refs.episodeHeadingLabelText != null)
            refs.episodeHeadingLabelText.text = model.EpisodeHeading;

        if (refs.bgImage != null && model.Bg != null)
            refs.bgImage.sprite = model.Bg;

        if (refs.bgOverlayImage != null && model.BgOverlay != null)
            refs.bgOverlayImage.sprite = model.BgOverlay;

        if (refs.chapterIndexLabelImage != null && model.ChapterIndexLabelSprite != null)
            refs.chapterIndexLabelImage.sprite = model.ChapterIndexLabelSprite;

        if (refs.episodeHeadingLabelImage != null && model.EpisodeHeadingLabelSprite != null)
            refs.episodeHeadingLabelImage.sprite = model.EpisodeHeadingLabelSprite;

        if (refs.chapterTitleLabelIconImage != null && model.TitleIcon != null)
            refs.chapterTitleLabelIconImage.sprite = model.TitleIcon;

        SetInteractable(model.Interactable && !model.Locked);
        SetLocked(model.Locked);
    }

    public void SetSelected(bool selected)
    {
        SetVisible(refs.selectedCanvasGroup, selected, blockRaycasts: false);
    }

    public void SetLocked(bool locked)
    {
        SetVisible(refs.lockedCanvasGroup, locked, blockRaycasts: locked);
    }

    public void SetInteractable(bool interactable)
    {
        if (refs.hitButton != null)
            refs.hitButton.interactable = interactable;
    }

    internal bool HasRequiredReferences()
    {
        return refs.HasRequired();
    }

    internal void AssignGeneratedReferences(References generatedRefs)
    {
        UnbindHitButton();

        refs = generatedRefs;

        RebindHitButton();
    }

    private void HandleClicked()
    {
        if (ChapterId < 0)
            return;

        Clicked?.Invoke(ChapterId);
    }

    private void RebindHitButton()
    {
        if (_boundHitButton == refs.hitButton)
            return;

        UnbindHitButton();

        if (refs.hitButton == null)
            return;

        _boundHitButton = refs.hitButton;
        _boundHitButton.onClick.AddListener(HandleClicked);
    }

    private void UnbindHitButton()
    {
        if (_boundHitButton == null)
            return;

        _boundHitButton.onClick.RemoveListener(HandleClicked);
        _boundHitButton = null;
    }

    private static void SetVisible(CanvasGroup group, bool visible, bool blockRaycasts)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = blockRaycasts;
    }
}