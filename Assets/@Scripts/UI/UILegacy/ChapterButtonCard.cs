using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// public readonly struct ChapterButtonCardModel
// {
//     public readonly int ChapterId;
//
//     public readonly string IndexText;         // 예: "1" or "챕터 5"
//     public readonly string ChapterIndexLabel;  // 예: "챕터5"
//     public readonly string ChapterTitle;       // 예: "짙은 밤에 드리운 불빛"
//     public readonly string EpisodeHeading;     // 예: "02 그녀의 선택"
//
//     public readonly Sprite Bg;
//     public readonly Sprite BgOverlay;
//     public readonly Sprite ChapterIndexLabelSprite;
//     public readonly Sprite EpisodeHeadingLabelSprite;
//     public readonly Sprite TitleIcon;
//
//     public readonly bool Interactable;
//     public readonly bool Locked;
//
//     public ChapterButtonCardModel(
//         int chapterId,
//         string indexText,
//         string chapterIndexLabel,
//         string chapterTitle,
//         string episodeHeading,
//         Sprite bg = null,
//         Sprite bgOverlay = null,
//         Sprite chapterIndexLabelSprite = null,
//         Sprite episodeHeadingLabelSprite = null,
//         Sprite titleIcon = null,
//         bool interactable = true,
//         bool locked = false)
//     {
//         ChapterId = chapterId;
//         IndexText = indexText;
//         ChapterIndexLabel = chapterIndexLabel;
//         ChapterTitle = chapterTitle;
//         EpisodeHeading = episodeHeading;
//         Bg = bg;
//         BgOverlay = bgOverlay;
//         ChapterIndexLabelSprite = chapterIndexLabelSprite;
//         EpisodeHeadingLabelSprite = episodeHeadingLabelSprite;
//         TitleIcon = titleIcon;
//         Interactable = interactable;
//         Locked = locked;
//     }
//
//     public static ChapterButtonCardModel Empty()
//         => new (
//             chapterId: -1,
//             indexText: "",
//             chapterIndexLabel: "",
//             chapterTitle: "",
//             episodeHeading: "",
//             interactable: false,
//             locked: true
//         );
// }

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
        //Selected_Root, // 선택 하이라이트가 있다면(없으면 제거)
        //Lock_Root,     // 잠금 표시(없으면 제거)
    }

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
        _hit       = View.Button(Refs.Hit_Button);
        _bg        = View.Image(Refs.Bg_Image);
        _bgOverlay = View.Image(Refs.BgOverlay_Image);

        _indexText             = View.Text(Refs.Index_Text);
        _chapterIndexLabelText = View.Text(Refs.ChapterIndexLabel_Text);
        _chapterTitleText      = View.Text(Refs.ChapterTitleLabel_Text);
        _episodeHeadingText    = View.Text(Refs.EpisodeHeadingLabel_Text);

        _chapterIndexLabelImage   = View.Image(Refs.ChapterIndexLabel_Image);
        _episodeHeadingLabelImage = View.Image(Refs.EpisodeHeadingLabel_Image);
        _titleIconImage           = View.Image(Refs.ChapterTitleLabelIcon_Image);

        //_selectedRoot = View.CanvasGroup(Refs.Selected_Root);
        //_lockRoot = View.CanvasGroup(Refs.Lock_Root);
        
        gameObject.SetActive(true);
    }

    public void BindClick(Action onClick)
    {
        if (_hit == null) return;

        _hit.onClick.RemoveAllListeners();
        _hit.onClick.AddListener(() => onClick());
    }

    public void Present(in ChapterButtonCardModel m)
    {
        ChapterId = m.ChapterId;

        if (_indexText != null)             _indexText.text             = m.IndexText;
        if (_chapterIndexLabelText != null) _chapterIndexLabelText.text = m.ChapterIndexLabel;
        if (_chapterTitleText != null)      _chapterTitleText.text      = m.ChapterTitle;
        if (_episodeHeadingText != null)    _episodeHeadingText.text    = m.EpisodeHeading;

        if (_bg != null && m.Bg != null)               _bg.sprite        = m.Bg;
        if (_bgOverlay != null && m.BgOverlay != null) _bgOverlay.sprite = m.BgOverlay;

        if (_chapterIndexLabelImage != null && m.ChapterIndexLabelSprite != null)     _chapterIndexLabelImage.sprite   = m.ChapterIndexLabelSprite;
        if (_episodeHeadingLabelImage != null && m.EpisodeHeadingLabelSprite != null) _episodeHeadingLabelImage.sprite = m.EpisodeHeadingLabelSprite;
        if (_titleIconImage != null && m.TitleIcon != null)                           _titleIconImage.sprite           = m.TitleIcon;

        SetInteractable(true);
        //SetInteractable(m.Interactable && !m.Locked);
        //SetLocked(m.Locked);
    }

    public void SetSelected(bool selected)
    {
        _selectedRoot.alpha = selected ? 1f : 0f;
        _selectedRoot.interactable = false;
        _selectedRoot.blocksRaycasts = false;
    }

    public void SetLocked(bool locked)
    {
        _lockRoot.alpha = locked ? 1f : 0f;
        _lockRoot.interactable = locked;
        _lockRoot.blocksRaycasts = locked;
    }

    public void SetInteractable(bool interactable)
    {
        if (_hit != null) _hit.interactable = interactable;
    }
}