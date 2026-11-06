namespace Ked.Samples.UI
{
    /// <summary>감상 · CG 탭. 이벤트 그림 격자. 내용과 잠김 · 열림은 빌드 명세의 고정 내용 — 여기서는 그리지 않는다. 크게 보기는 아직 없다.</summary>
    [UIViewKey("Ked.CGPage")]
    public sealed class CGPage : UIPage<CGPage.Refs>
    {
        public enum Refs
        {
            CGInfo,
            CGGrid,
        }
    }
}
