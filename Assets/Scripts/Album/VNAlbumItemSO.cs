using UnityEngine;

[CreateAssetMenu(
    fileName = "AlbumItem_New",
    menuName = "VN/Album/Album Item")]
public sealed class VNAlbumItemSO : ScriptableObject
{
    [SerializeField]
    private string key;

    [SerializeField]
    private string title;

    [SerializeField]
    private Sprite cgSprite;

    [SerializeField]
    private Sprite thumbnailSprite;

    public string Key => key;
    public string Title => title;

    public Sprite CgSprite => cgSprite;

    public Sprite Thumbnail =>
        thumbnailSprite != null
            ? thumbnailSprite
            : cgSprite;
}