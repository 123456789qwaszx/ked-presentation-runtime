using System.Collections.Generic;

// 로컬 전용 envelope. 서버에는 Snapshot만 보낸다.
// playthroughs-v3에 기록한다. 구형식 마이그레이션은 지원하지 않는다.
public sealed class PlaythroughFile
{
    public int FormatVersion = 3;
    public LocalSaveFile Snapshot;
    public long LocalCommitVersion;
    public long SyncedCommitVersion;
    public PlaythroughSyncState Sync = new();
    // 응답 유실 시 새 snapshot과 합치지 않고 같은 요청을 다시 보낸다.
    public SyncWork InFlight;
    public string ReleasedTo;
}

public sealed class SyncWork
{
    public string Id;
    public string PlaythroughId;
    public long CommitVersion;
    public long BaseRevision;
    public int? ChapterVersion;
    public LocalSaveFile Snapshot;
    public List<PendingChoice> Choices = new();
    public List<PendingEvent> Events = new();
}

// 다중 파일 이동의 재실행 기록. Destination ID는 처음부터 고정한다.
public sealed class ConflictTransfer
{
    public string SourceId;
    public string DestinationId;
    public PlaythroughFile Destination;
    public long SelectionVersion;
    public bool WasActive;
}

public sealed class RestoreProgress
{
    public bool Started;
    public bool Completed;
    public bool AllowActivation = true;
}
