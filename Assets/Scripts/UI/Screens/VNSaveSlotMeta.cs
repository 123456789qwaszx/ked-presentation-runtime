public sealed class VNSaveSlotMeta
{
    public bool IsEmpty;

    public string Label;
    public string Preview;

    public string ChapterId;
    public string SavedAtUtc;

    public int PlaySeconds;

    public static VNSaveSlotMeta From(SaveSlotEntry slot)
    {
        return new VNSaveSlotMeta
        {
            Label = slot.Label,
            Preview = slot.Preview,
            ChapterId = slot.ChapterId,
            SavedAtUtc = slot.SavedAtUtc,
            PlaySeconds = slot.PlaySeconds,
        };
    }

    public static VNSaveSlotMeta Empty()
    {
        return new VNSaveSlotMeta
        {
            IsEmpty = true,
        };
    }
}