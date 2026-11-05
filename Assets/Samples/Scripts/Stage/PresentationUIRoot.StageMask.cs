public sealed partial class PresentationUIRoot : IStageMaskProvider
{
    private StageMaskSlot[] _stageMaskSlots;

    public StageMaskSlot GetStageMaskSlot(PresentationStageKey stage)
        => _stageMaskSlots[(int)stage];

    private void CacheStageMaskProviderRefs()
    {
        _stageMaskSlots = new StageMaskSlot[PresentationStageCount];

        _stageMaskSlots[(int)PresentationStageKey.Stage00] = RequireMaskSlot(Refs.Stage00Mask_Root);
        _stageMaskSlots[(int)PresentationStageKey.Stage01] = RequireMaskSlot(Refs.Stage01Mask_Root);
        _stageMaskSlots[(int)PresentationStageKey.Stage02] = RequireMaskSlot(Refs.Stage02Mask_Root);
    }

    private StageMaskSlot RequireMaskSlot(Refs key)
    {
        StageMaskSlot slot = View.Component<StageMaskSlot>(key);

        if (slot == null || !slot.HasMask)
            throw new System.InvalidOperationException($"[PresentationUIRoot] 무대 마스크 슬롯이 없거나 Graphic · Mask가 비었다: {key}");

        return slot;
    }
}