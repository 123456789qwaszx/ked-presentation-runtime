using UnityEngine;

[System.Serializable]
public sealed class ChapterButtonCardBuildOptions
{
    [Header("Size")]
    public Vector2 defaultCardSize = new Vector2(400f, 700f);

    [Header("Sprites")]
    public Sprite defaultBgSprite;
    public Sprite defaultBgOverlaySprite;
    public Sprite defaultChapterIndexLabelSprite;
    public Sprite defaultEpisodeHeadingLabelSprite;
    public Sprite defaultTitleIconSprite;

    [Header("Text")]
    public int indexFontSize = 36;
    public int chapterIndexFontSize = 18;
    public int titleFontSize = 24;
    public int episodeHeadingFontSize = 18;

    [Header("State")]
    public bool hideSelectedByDefault = true;
    public bool hideLockedByDefault = true;
}