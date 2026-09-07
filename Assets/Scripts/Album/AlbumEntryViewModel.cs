using UnityEngine;

// Album UI가 그리기 위해 필요한 정보만 담는다.
// 데이터 출처가 SO인지 JSON인지 서버인지 UI는 모른다.
public sealed class AlbumEntryViewModel
{
    public string Id;

    public string Title;

    public Sprite Thumbnail;
    public Sprite FullImage;

    public bool IsUnlocked;
}