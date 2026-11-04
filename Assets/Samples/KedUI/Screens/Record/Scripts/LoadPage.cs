namespace Ked.Samples.UI
{
    /// <summary>기록 · 불러오기 탭. 몸은 SaveSlotsPage, 여기는 이 탭의 글자만.</summary>
    [UIViewKey("Ked.LoadPage")]
    public sealed class LoadPage : SaveSlotsPage
    {
        protected override string ReadyHintKey => "slots.hint.load";
    }
}
