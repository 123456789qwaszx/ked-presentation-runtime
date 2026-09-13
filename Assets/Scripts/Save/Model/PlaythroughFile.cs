// 회차 파일 envelope. FormatVersion은 저장 JSON의 모양만 판별한다.
public sealed class PlaythroughFile
{
    public int FormatVersion;
    public LocalSaveFile Snapshot;
}

public static class SaveFormat
{
    public const int PlaythroughVersion = 3;
    public const int SaveSlotVersion = 1;
}
