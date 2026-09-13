public sealed class VNSaveSlotMeta
{
    public string Id;
    public bool IsNewSlot;

    public string Label;
    public string Preview;

    public string ChapterId;
    public string SavedAtUtc;

    public int PlaySeconds;

    public static VNSaveSlotMeta ExistingSlot(SaveSlotEntry slot)
    {
        return new VNSaveSlotMeta
        {
            Id = slot.Id,
            IsNewSlot = false,

            Label = slot.Label,
            Preview = slot.Preview,

            ChapterId = slot.ChapterId,
            SavedAtUtc = slot.SavedAtUtc,

            PlaySeconds = slot.PlaySeconds,
        };
    }

    public static VNSaveSlotMeta NewSlot()
    {
        return new VNSaveSlotMeta
        {
            IsNewSlot = true,
        };
    }
}