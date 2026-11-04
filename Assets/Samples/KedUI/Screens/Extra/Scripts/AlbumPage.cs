namespace Ked.Samples.UI
{
    /// <summary>감상 · 앨범 탭. 지나온 장소를 모은 카드. 내용과 잠김 · 열림은 빌드 명세의 고정 내용 — 여기서는 그리지 않는다.</summary>
    [UIViewKey("Ked.AlbumPage")]
    public sealed class AlbumPage : UIPage<AlbumPage.Refs>
    {
        public enum Refs
        {
            AlbumInfo,
            AlbumGrid,
        }
    }
}
