using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChapterButtonCard : UIBase<ChapterButtonCard.Refs>
{
    public enum Refs
    {
        Bg_Root,
        Bg_Pad,
        Bg_Image,

        BgOverlay_Root,
        BgOverlay_Pad,
        BgOverlay_Image,

        Index_Root,
        Index_Anchor,
        Index_Text,

        HeadingBlock_Root,

        ChapterIndexLabel_Root,
        ChapterIndexLabel_Image,
        ChapterIndexLabel_Text,

        ChapterTitleLabel_Root,
        ChapterTitleLabelBG_Image,
        ChapterTitleLabelIcon_Image,
        ChapterTitleLabel_Text,

        EpisodeHeadingLabel_Root,
        EpisodeHeadingLabel_Image,
        EpisodeHeadingLabel_Text,

        Hit_Button,
        Selected_Root,
        // Lock_Root,
    }

    public event Action<int> Clicked;

    private Button _hit;
    private Image _bg;
    private Image _bgOverlay;

    private TMP_Text _indexText;
    private TMP_Text _chapterIndexLabelText;
    private TMP_Text _chapterTitleText;
    private TMP_Text _episodeHeadingText;

    private Image _chapterIndexLabelImage;
    private Image _episodeHeadingLabelImage;
    private Image _titleIconImage;

    private CanvasGroup _selectedRoot;
    private CanvasGroup _lockRoot;

    public int ChapterId { get; private set; } = -1;

    protected override void OnInitialize()
    {
        _hit = View.Button(Refs.Hit_Button);
        _bg = View.Image(Refs.Bg_Image);
        _bgOverlay = View.Image(Refs.BgOverlay_Image);

        _indexText = View.Text(Refs.Index_Text);
        _chapterIndexLabelText = View.Text(Refs.ChapterIndexLabel_Text);
        _chapterTitleText = View.Text(Refs.ChapterTitleLabel_Text);
        _episodeHeadingText = View.Text(Refs.EpisodeHeadingLabel_Text);

        _chapterIndexLabelImage = View.Image(Refs.ChapterIndexLabel_Image);
        _episodeHeadingLabelImage = View.Image(Refs.EpisodeHeadingLabel_Image);
        _titleIconImage = View.Image(Refs.ChapterTitleLabelIcon_Image);

        _selectedRoot = View.CanvasGroup(Refs.Selected_Root);
        //_lockRoot = View.CanvasGroup(Refs.Lock_Root);

        if (_hit != null)
            _hit.onClick.AddListener(HandleClicked);

        gameObject.SetActive(true);
    }

    public void Present(in ChapterButtonCardModel m)
    {
        ChapterId = m.ChapterId;

        if (_indexText != null)
            _indexText.text = m.IndexText;

        if (_chapterIndexLabelText != null)
            _chapterIndexLabelText.text = m.ChapterIndexLabel;

        if (_chapterTitleText != null)
            _chapterTitleText.text = m.ChapterTitle;

        if (_episodeHeadingText != null)
            _episodeHeadingText.text = m.EpisodeHeading;

        if (_bg != null)
            _bg.sprite = m.Bg;

        if (_bgOverlay != null)
            _bgOverlay.sprite = m.BgOverlay;

        if (_chapterIndexLabelImage != null)
            _chapterIndexLabelImage.sprite = m.ChapterIndexLabelSprite;

        if (_episodeHeadingLabelImage != null)
            _episodeHeadingLabelImage.sprite = m.EpisodeHeadingLabelSprite;

        if (_titleIconImage != null)
            _titleIconImage.sprite = m.TitleIcon;

        SetInteractable(m.Interactable && !m.Locked);
        SetLocked(m.Locked);
    }

    public void SetSelected(bool selected)
    {
        if (_selectedRoot != null)
            _selectedRoot.SetVisible(selected, blockRaycasts: false);
    }

    public void SetLocked(bool locked)
    {
        if (_lockRoot == null)
            return;

        _lockRoot.alpha = locked ? 1f : 0f;
        _lockRoot.interactable = locked;
        _lockRoot.blocksRaycasts = locked;
    }

    public void SetInteractable(bool interactable)
    {
        if (_hit != null)
            _hit.interactable = interactable;
    }

    private void HandleClicked()
    {
        if (ChapterId < 0)
            return;

        Clicked?.Invoke(ChapterId);
    }
}