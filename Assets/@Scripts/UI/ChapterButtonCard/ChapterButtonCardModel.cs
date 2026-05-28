using UnityEngine;

public readonly struct ChapterButtonCardModel
{
    public readonly string ChapterId;

    public readonly string IndexText;
    public readonly string ChapterIndexLabel;
    public readonly string ChapterTitle;
    public readonly string EpisodeHeading;

    public readonly Sprite Bg;
    public readonly Sprite BgOverlay;
    public readonly Sprite ChapterIndexLabelSprite;
    public readonly Sprite EpisodeHeadingLabelSprite;
    public readonly Sprite TitleIcon;

    public readonly bool Interactable;
    public readonly bool Locked;

    public ChapterButtonCardModel(
        string chapterId,
        string indexText,
        string chapterIndexLabel,
        string chapterTitle,
        string episodeHeading,
        Sprite bg = null,
        Sprite bgOverlay = null,
        Sprite chapterIndexLabelSprite = null,
        Sprite episodeHeadingLabelSprite = null,
        Sprite titleIcon = null,
        bool interactable = true,
        bool locked = false)
    {
        ChapterId = chapterId;
        IndexText = indexText;
        ChapterIndexLabel = chapterIndexLabel;
        ChapterTitle = chapterTitle;
        EpisodeHeading = episodeHeading;
        Bg = bg;
        BgOverlay = bgOverlay;
        ChapterIndexLabelSprite = chapterIndexLabelSprite;
        EpisodeHeadingLabelSprite = episodeHeadingLabelSprite;
        TitleIcon = titleIcon;
        Interactable = interactable;
        Locked = locked;
    }

    public static ChapterButtonCardModel Empty()
    {
        return new ChapterButtonCardModel(
            chapterId: "",
            indexText: "",
            chapterIndexLabel: "",
            chapterTitle: "",
            episodeHeading: "",
            interactable: false,
            locked: true);
    }
}