public sealed class VNSaveSlotMeta
{
    public bool IsEmpty;

    public string Label;
    public string Preview;

    public string ChapterId;
    public string SavedAtUtc;

    public int PlaySeconds;

    // 서버에서 summary만 복구되어
    // 실제 Bookmark snapshot은 아직 로컬에 없는 상태.
    public bool RequiresDownload;

    // 서버가 거절해 자동 동기화가 멈춘 상태.
    public bool HasSyncError;
}