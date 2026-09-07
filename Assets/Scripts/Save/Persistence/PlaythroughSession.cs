using System;
using System.Collections.Generic;

// 경로와 회차 ID가 수명 동안 고정된다. 한 repository에서 회차당 하나만 연다.
// 쓰기는 사본에 적용 → 디스크 교체 성공 → 메모리 채택. await는 이 경계 밖에서만 한다.
public sealed class PlaythroughSession
{
    private readonly object _gate = new();
    private readonly string _path;
    private readonly Action<string, string> _write;
    private PlaythroughFile _file;
    private bool _transferPending;
    private bool _closed;
    internal void Close() { lock (_gate) _closed = true; }
    internal void SuspendCommits() { lock (_gate) _transferPending = true; }
    public string Id { get; }

    internal PlaythroughSession(string id, string path, PlaythroughFile file, Action<string, string> write)
    {
        Id = id;
        _path = path;
        _file = file;
        _write = write;
    }

    internal static T Copy<T>(T value) => SaveJson.Deserialize<T>(SaveJson.Serialize(value));
    public PlaythroughFile Read() { lock (_gate) return Copy(_file); }
    public int NextSeq { get { lock (_gate) return _file.Sync.NextSeq; } }
    public bool NeedsSync
    {
        get
        {
            lock (_gate)
                return !_closed && _file.ReleasedTo == null && _file.Sync.ConflictedAtUtc == null && _file.Sync.BlockedReason == null
                    && (_file.InFlight != null || _file.LocalCommitVersion > _file.SyncedCommitVersion);
        }
    }

    private void Update(Action<PlaythroughFile> change)
    {
        lock (_gate)
        {
            if (_closed) throw new InvalidOperationException("정리된 회차에는 쓸 수 없다.");
            PlaythroughFile next = Copy(_file);
            change(next);
            _write(_path, SaveJson.SerializePretty(next));
            _file = next;
        }
    }

    internal void Persist() => Update(_ => { });

    public void Commit(LocalSaveFile snapshot, IReadOnlyList<PendingChoice> choices, IReadOnlyList<PendingEvent> events)
    {
        if (snapshot.PlaythroughId != Id) throw new InvalidOperationException("회차가 다른 snapshot이다.");
        Update(next =>
        {
            if (_transferPending || next.ReleasedTo != null) throw new InvalidOperationException("이미 갈라진 source 회차에는 쓸 수 없다.");
            next.Snapshot = Copy(snapshot);
            next.LocalCommitVersion++;
            foreach (PendingChoice choice in choices)
            {
                PendingChoice item = Copy(choice);
                item.Seq = next.Sync.NextSeq++;
                next.Sync.PendingChoices.Add(item);
            }
            foreach (PendingEvent item in events) next.Sync.PendingEvents.Add(Copy(item));
        });
    }

    public SyncWork CaptureSyncWork()
    {
        lock (_gate)
        {
            if (!NeedsSync) return null;
            if (_file.InFlight == null)
                Update(next => next.InFlight = new SyncWork
                {
                    Id = Guid.NewGuid().ToString("N"),
                    PlaythroughId = Id,
                    CommitVersion = next.LocalCommitVersion,
                    BaseRevision = next.Sync.BaseRevision ?? 0,
                    Snapshot = Copy(next.Snapshot),
                    Choices = Copy(next.Sync.PendingChoices),
                    Events = Copy(next.Sync.PendingEvents),
                });
            return Copy(_file.InFlight);
        }
    }

    public void SetServerId(long id) => Update(next => next.Sync.PlaythroughId = id);
    public void SetChapterVersion(string workId, int version) => Update(next =>
    {
        RequireWork(next, workId);
        next.InFlight.ChapterVersion = version;
    });

    public void Acknowledge(string workId, long revision)
    {
        Update(next =>
        {
            RequireWork(next, workId);
            SyncWork sent = next.InFlight;
            // 전송한 접두부만 제거하고 후속 커밋은 남긴다.
            next.Sync.PendingChoices.RemoveRange(0, sent.Choices.Count);
            next.Sync.PendingEvents.RemoveRange(0, sent.Events.Count);
            next.Sync.BaseRevision = revision;
            next.Sync.SyncedSceneCount = sent.Snapshot.Scenes?.Count ?? 0;
            next.SyncedCommitVersion = sent.CommitVersion;
            next.InFlight = null;
            next.Sync.RetryCount = 0;
            next.Sync.RetryAfterUtc = null;
            next.Sync.BlockedReason = null;
        });
    }

    public void MarkConflicted(string now) => Update(next => next.Sync.ConflictedAtUtc = now);

    internal void ReleaseTo(string destinationId) => Update(next =>
    {
        if (next.ReleasedTo != null && next.ReleasedTo != destinationId)
            throw new InvalidOperationException("다른 fork로 이미 이동했다.");
        next.ReleasedTo = destinationId;
        next.Sync.PendingChoices.Clear();
        next.Sync.PendingEvents.Clear();
        next.InFlight = null;
        next.Sync.ConflictedAtUtc = null;
        next.Sync.BlockedReason = null;
        next.Sync.RetryAfterUtc = null;
    });


    public PlaythroughSummary GetSummary()
    {
        lock (_gate)
        {
            LocalSaveFile save = _file.Snapshot;
            return new PlaythroughSummary
            {
                PlaythroughId = Id, ForkedFrom = Copy(save.ForkedFrom), ChapterId = save.ChapterId,
                CurrentEpisodeId = save.CurrentEpisodeId, ChapterCompleted = save.ChapterCompleted,
                SceneCount = save.Scenes?.Count ?? 0, InheritedPlaySeconds = save.InheritedPlaySeconds,
                OwnPlaySeconds = save.OwnPlaySeconds, SavedAtUtc = save.SavedAtUtc,
            };
        }
    }

    internal bool CanCollect(bool requireSynced)
    {
        lock (_gate)
        {
            if (_closed || _transferPending && _file.ReleasedTo == null || _file.InFlight != null) return false;
            if (_file.Sync.ConflictedAtUtc != null || _file.Sync.BlockedReason != null) return false;
            if (_file.ReleasedTo != null) return true;
            return !requireSynced || (_file.LocalCommitVersion == _file.SyncedCommitVersion
                && _file.Sync.PendingChoices.Count == 0 && _file.Sync.PendingEvents.Count == 0);
        }
    }

    public bool CanRetry(DateTime now)
    {
        lock (_gate) return NeedsSync && (_file.Sync.RetryAfterUtc == null
            || DateTime.Parse(_file.Sync.RetryAfterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind) <= now);
    }

    public void RecordFailure(DateTime now, string blockedReason = null) => Update(next =>
    {
        next.Sync.RetryCount = Math.Min(10, next.Sync.RetryCount + 1);
        double seconds = Math.Min(300, 5 * Math.Pow(2, next.Sync.RetryCount - 1));
        next.Sync.RetryAfterUtc = now.AddSeconds(seconds).ToString("o");
        next.Sync.BlockedReason = blockedReason;
    });

    public void ResetRetry() => Update(next =>
    {
        next.Sync.RetryCount = 0;
        next.Sync.RetryAfterUtc = null;
        next.Sync.BlockedReason = null;
    });

    private static void RequireWork(PlaythroughFile file, string workId)
    {
        if (file.InFlight == null || file.InFlight.Id != workId)
            throw new InvalidOperationException("현재 전송 작업과 응답이 일치하지 않는다.");
    }
}
