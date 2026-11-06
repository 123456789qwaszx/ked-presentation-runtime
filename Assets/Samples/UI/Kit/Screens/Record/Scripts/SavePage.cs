namespace Ked.Samples.UI
{
    /// <summary>기록 · 저장 탭. 몸은 SaveSlotsPage, 여기는 이 탭의 글자만.</summary>
    [UIViewKey("Ked.SavePage")]
    public sealed class SavePage : SaveSlotsPage
    {
        protected override string ReadyHintKey => "slots.hint.save";
    }
}
