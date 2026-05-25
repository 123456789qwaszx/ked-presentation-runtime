using UnityEngine;

[CreateAssetMenu(
    fileName = "ChapterButtonCardStyle",
    menuName = "VN/UI/Chapter Button Card Style")]
public sealed class ChapterButtonCardStyleSO : ScriptableObject
{
    [Header("Size")]
    public Vector2 defaultCardSize = new Vector2(360f, 160f);

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