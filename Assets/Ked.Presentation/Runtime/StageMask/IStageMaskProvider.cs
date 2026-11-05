public interface IStageMaskProvider
{
    StageMaskSlot GetStageMaskSlot(PresentationStageKey stage);
}

public static class StageMaskProviderExtensions
{
    // 장면 경계 — 무대마다 tx 모션을 끊고 씬에 저장된 기준 모양으로.
    public static void ResetAllToBaseline(this IStageMaskProvider provider)
    {
        for (int i = 0; i < (int)PresentationStageKey.Count; i++)
            provider.GetStageMaskSlot((PresentationStageKey)i).ResetToBaseline();
    }
}
