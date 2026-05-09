using System;

[Serializable]
public sealed class VNSaveSlotMeta
{
    public string slotId = "";
    public bool isEmpty = true;
    public bool isAutoSlot = false;

    public string chapterLabel = "";
    public string linePreview = "";
    public string savedAt = "";
    public long savedAtTicks = 0L;
    public int playtimeSeconds = 0;

    public static VNSaveSlotMeta Empty(string slotId)
    {
        return new VNSaveSlotMeta
        {
            slotId = slotId,
            isEmpty = true,
            isAutoSlot = slotId == "auto"
        };
    }

    public static VNSaveSlotMeta FromSaveData(VNSaveData data)
    {
        if (data == null)
            return Empty("");

        data.Normalize();

        return new VNSaveSlotMeta
        {
            slotId = data.slotId,
            isEmpty = false,
            isAutoSlot = data.slotId == "auto",
            chapterLabel = data.chapterLabel,
            linePreview = data.linePreview,
            savedAt = data.savedAt,
            savedAtTicks = data.savedAtTicks,
            playtimeSeconds = data.playtimeSeconds
        };
    }

    public string FormatPlaytime()
    {
        TimeSpan ts = TimeSpan.FromSeconds(playtimeSeconds);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}