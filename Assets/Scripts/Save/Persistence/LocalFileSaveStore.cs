using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Unity 메인 스레드에서 사용하는 repository. 회차 파일의 수정은 Session만 수행한다.
public sealed class LocalFileSaveStore : ILocalSaveStore
{
    private sealed class ActiveFile
    {
        public string ActiveId;
        public long SelectionVersion;
    }

    private readonly string _directory;
    private readonly Dictionary<string, PlaythroughSession> _sessions = new(StringComparer.Ordinal);
    private readonly Action<string, string> _write;
    private bool _initialized;
    private string DataDirectory => Path.Combine(_directory, "playthroughs-v3");
    private string ActivePath => Path.Combine(_directory, "active.json");
    private string TransferPath => Path.Combine(_directory, "conflict-transfer.json");
    private string RestorePath => Path.Combine(_directory, "restore-progress.json");

    public LocalFileSaveStore(string directory, Action<string, string> write = null)
    {
        _directory = directory;
        _write = write ?? AtomicFile.WriteAllText;
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'))
            throw new ArgumentException("잘못된 회차 ID", nameof(id));
    }

    private string PathOf(string id) { ValidateId(id); return Path.Combine(DataDirectory, id + ".json"); }
    private T Read<T>(string path) where T : class =>
        AtomicFile.ReadAllTextOrNull(path) is string json ? SaveJson.Deserialize<T>(json) : null;
    private void Write(string path, object value) => _write(path, SaveJson.SerializePretty(value));
    private ActiveFile ReadActive() => Read<ActiveFile>(ActivePath) ?? new ActiveFile();
    public string ActiveId => ReadActive().ActiveId;

    // 로컬 초기화에서는 중단된 회차 이동만 복구한다. 구형식 마이그레이션은 지원하지 않는다.
    public void Initialize()
    {
        if (_initialized) return;
        RecoverTransfer();
        _initialized = true;
    }

    private static void Validate(PlaythroughFile file, string id)
    {
        if (file == null || file.FormatVersion != 3 || file.Snapshot?.PlaythroughId != id || file.Sync == null)
            throw new InvalidDataException("지원하지 않거나 손상된 회차 파일: " + id);
    }

    public PlaythroughSession Open(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_sessions.TryGetValue(id, out PlaythroughSession session)) return session;
        PlaythroughFile file = Read<PlaythroughFile>(PathOf(id));
        if (file == null) return null;
        Validate(file, id);
        session = new PlaythroughSession(id, PathOf(id), file, _write);
        _sessions.Add(id, session);
        return session;
    }

    public PlaythroughSession Create(LocalSaveFile save)
    {
        if (Open(save.PlaythroughId) != null) throw new InvalidOperationException("이미 존재하는 회차다.");
        var file = new PlaythroughFile { Snapshot = PlaythroughSession.Copy(save), LocalCommitVersion = 1 };
        Write(PathOf(save.PlaythroughId), file);
        return Open(save.PlaythroughId);
    }

    public void ImportRestored(LocalSaveFile save, long serverId, long revision, int nextSeq)
    {
        if (Open(save.PlaythroughId) != null) return; // await 동안 생긴 로컬 진행도 덮지 않는다.
        Write(PathOf(save.PlaythroughId), new PlaythroughFile
        {
            Snapshot = save, LocalCommitVersion = 1, SyncedCommitVersion = 1,
            Sync = new PlaythroughSyncState { PlaythroughId = serverId, BaseRevision = revision,
                NextSeq = nextSeq, SyncedSceneCount = save.Scenes?.Count ?? 0 },
        });
    }

    public void SetActive(string id)
    {
        if (Open(id) == null) throw new InvalidOperationException("파일이 없는 회차는 active로 지정할 수 없다.");
        ActiveFile active = ReadActive();
        active.ActiveId = id;
        active.SelectionVersion++;
        Write(ActivePath, active);
    }

    public void SelectLocalPlaythrough()
    {
        RestoreProgress progress = LoadRestoreProgress();
        progress.AllowActivation = false;
        SaveRestoreProgress(progress);
        ActiveFile active = ReadActive();
        active.SelectionVersion++;
        Write(ActivePath, active);
    }

    public bool TryActivateRestored(string id)
    {
        if (!LoadRestoreProgress().AllowActivation || ActiveId != null || Open(id) == null) return false;
        SetActive(id);
        return true;
    }

    public LocalSaveFile LoadActive() => LoadPlaythrough(ActiveId);
    public LocalSaveFile LoadPlaythrough(string id) => Open(id)?.Read().Snapshot;
    public IReadOnlyList<string> ListPlaythroughIds() => !Directory.Exists(DataDirectory)
        ? Array.Empty<string>()
        : Directory.GetFiles(DataDirectory, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(id => id, StringComparer.Ordinal).ToArray();
    public BookmarkFile LoadBookmarks() => Read<BookmarkFile>(Path.Combine(_directory, "bookmarks.json")) ?? new BookmarkFile();
    public void SaveBookmarks(BookmarkFile file) => Write(Path.Combine(_directory, "bookmarks.json"), file);
    public RestoreProgress LoadRestoreProgress() => Read<RestoreProgress>(RestorePath) ?? new RestoreProgress();
    public void SaveRestoreProgress(RestoreProgress progress) => Write(RestorePath, progress);

    public PlaythroughSession ForkConflict(string sourceId)
    {
        // 이전 시도가 I/O 오류로 중단됐으면 먼저 같은 destination으로 마무리한다.
        RecoverTransfer();
        PlaythroughSession source = Open(sourceId) ?? throw new InvalidOperationException("충돌 회차가 없다.");
        PlaythroughFile current = source.Read();
        if (current.ReleasedTo != null) return Open(current.ReleasedTo);
        string newId = Guid.NewGuid().ToString("N");
        LocalSaveFile save = current.Snapshot;
        int sceneIndex = current.Sync.SyncedSceneCount;
        save.PlaythroughId = newId;
        save.ForkedFrom = new ForkOrigin { PlaythroughId = sourceId, SceneIndex = sceneIndex };
        save.SavedAtUtc = DateTime.UtcNow.ToString("o");
        if (save.Scenes != null && sceneIndex < save.Scenes.Count)
        {
            save.InheritedPlaySeconds = save.Scenes[sceneIndex].Checkpoint.PlaySecondsAtEntry;
            save.OwnPlaySeconds = Math.Max(0, save.PlaySeconds - save.InheritedPlaySeconds);
        }
        var sync = new PlaythroughSyncState();
        foreach (PendingChoice choice in current.Sync.PendingChoices)
        {
            choice.Seq = sync.NextSeq++;
            sync.PendingChoices.Add(choice);
        }
        sync.PendingEvents.AddRange(current.Sync.PendingEvents);
        ActiveFile active = ReadActive();
        Write(TransferPath, new ConflictTransfer
        {
            SourceId = sourceId, DestinationId = newId,
            Destination = new PlaythroughFile { Snapshot = save, Sync = sync, LocalCommitVersion = 1 },
            SelectionVersion = active.SelectionVersion, WasActive = active.ActiveId == sourceId,
        });
        source.SuspendCommits();
        RecoverTransfer();
        return Open(newId);
    }

    private void RecoverTransfer()
    {
        ConflictTransfer transfer = Read<ConflictTransfer>(TransferPath);
        if (transfer == null) return;
        // destination을 먼저 보존한다. 이미 존재하면 절대 초기 상태로 덮지 않는다.
        if (Open(transfer.DestinationId) == null)
            Write(PathOf(transfer.DestinationId), transfer.Destination);
        PlaythroughSession source = Open(transfer.SourceId) ?? throw new InvalidDataException("갈라지기 source가 없다.");
        source.ReleaseTo(transfer.DestinationId);
        ActiveFile active = ReadActive();
        if (transfer.WasActive && active.ActiveId == transfer.SourceId && active.SelectionVersion == transfer.SelectionVersion)
            SetActive(transfer.DestinationId);
        File.Delete(TransferPath);
    }
}
