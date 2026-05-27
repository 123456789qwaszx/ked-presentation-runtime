using System;
using UnityEngine;

[Serializable]
public sealed class ChapterCardEntry
{
    [Header("Identity")]
    [SerializeField] private int chapterId;

    public int ChapterId => chapterId;
    [Header("Text")]
    [SerializeField] private string indexText = "01";
    [SerializeField] private string chapterIndexLabel = "챕터 01";
    [SerializeField] private string chapterTitle = "Chapter Title";
    [SerializeField] private string episodeHeading = "";

    [Header("Sprites")]
    [SerializeField] private Sprite bg;
    [SerializeField] private Sprite bgOverlay;
    [SerializeField] private Sprite chapterIndexLabelSprite;
    [SerializeField] private Sprite episodeHeadingLabelSprite;
    [SerializeField] private Sprite titleIcon;

    [Header("State")]
    [SerializeField] private bool interactable = true;
    [SerializeField] private bool locked;

    public ChapterEpisodeProgressionSO EpisodeProgression;
    public ChapterCardEntry()
    {
    }

    public ChapterCardEntry(
        int chapterId,
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
        this.chapterId = chapterId;
        this.indexText = indexText;
        this.chapterIndexLabel = chapterIndexLabel;
        this.chapterTitle = chapterTitle;
        this.episodeHeading = episodeHeading;
        this.bg = bg;
        this.bgOverlay = bgOverlay;
        this.chapterIndexLabelSprite = chapterIndexLabelSprite;
        this.episodeHeadingLabelSprite = episodeHeadingLabelSprite;
        this.titleIcon = titleIcon;
        this.interactable = interactable;
        this.locked = locked;
    }

    public ChapterButtonCardModel CreateModel()
    {
        return new ChapterButtonCardModel(
            chapterId: chapterId,
            indexText: indexText,
            chapterIndexLabel: chapterIndexLabel,
            chapterTitle: chapterTitle,
            episodeHeading: episodeHeading,
            bg: bg,
            bgOverlay: bgOverlay,
            chapterIndexLabelSprite: chapterIndexLabelSprite,
            episodeHeadingLabelSprite: episodeHeadingLabelSprite,
            titleIcon: titleIcon,
            interactable: interactable && !locked,
            locked: locked);
    }
}