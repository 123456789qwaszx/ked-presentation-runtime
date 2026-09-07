using UnityEngine;

[CreateAssetMenu(fileName = "AlbumItem_New", menuName = "VN/Album/Album Item")]
public sealed class VNAlbumItemSO : ScriptableObject
{
    [Tooltip("고유 키. Unlock에 전달하는 값과 일치해야 한다.")]
    public string key;

    [Tooltip("앨범 확대 화면에 표시할 원본 CG")]
    public Sprite cgSprite;

    [Tooltip("그리드 썸네일. 비워두면 cgSprite 사용")]
    public Sprite thumbnailSprite;

    public string title;

    public Sprite GetThumbnail()
    {
        return thumbnailSprite != null ? thumbnailSprite : cgSprite;
    }
}