public interface IStageMaskProvider
{
    StageMaskSlot GetStageMaskSlot(PresentationStageKey stage);

    bool TryGetStageMaskSlot(
        PresentationStageKey stage,
        out StageMaskSlot slot);

    bool TryGetStageMaskGraphic(
        PresentationStageKey stage,
        out StageMaskGraphic graphic);
}

public sealed partial class PresentationUIRoot : IStageMaskProvider
{
    private StageMaskSlot[] _stageMaskSlots;

    public StageMaskSlot GetStageMaskSlot(PresentationStageKey stage)
        => _stageMaskSlots[(int)stage];

    public bool TryGetStageMaskSlot(
        PresentationStageKey stage,
        out StageMaskSlot slot)
    {
        slot = _stageMaskSlots[(int)stage];
        return slot != null;
    }

    public bool TryGetStageMaskGraphic(
        PresentationStageKey stage,
        out StageMaskGraphic graphic)
    {
        graphic = null;

        StageMaskSlot slot = _stageMaskSlots[(int)stage];

        if (slot == null)
            return false;

        graphic = slot.Graphic;
        return graphic != null;
    }

    private void CacheStageMaskProviderRefs()
    {
        _stageMaskSlots = new StageMaskSlot[PresentationStageCount];

        _stageMaskSlots[(int)PresentationStageKey.Stage00] =
            View.Component<StageMaskSlot>(Refs.Stage00Mask_Root);

        _stageMaskSlots[(int)PresentationStageKey.Stage01] =
            View.Component<StageMaskSlot>(Refs.Stage01Mask_Root);

        _stageMaskSlots[(int)PresentationStageKey.Stage02] =
            View.Component<StageMaskSlot>(Refs.Stage02Mask_Root);
    }
}