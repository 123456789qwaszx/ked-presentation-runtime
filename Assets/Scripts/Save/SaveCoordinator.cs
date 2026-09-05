using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Bridges progression reports into the save layer.
// This is the save layer's only dependency on progression-specific types.
//
// 저장 단위는 장면이다 (G4). 장면 끝 fold 한 번에:
// 1) 현재 상태를 먼저 보존    / localStore.Save(...)  — 장면 진입 스냅샷 또는 챕터 완료
// 2) 서버용 이력을 남김       / _queue.EnqueueChoice/Event(...)  — 확정 순서대로
// 3) 서버 전송은 기다리지 않음 / _server.TrySyncAsync(...)
//
// 회차 파일은 이력(Scenes)을 든다 (F1). 장면 진입에서 스냅샷을 받아 두고, 장면 끝에 경로를 붙여
// 장면 기록 하나로 접는다. 이력은 현재 챕터 안에서만 쌓이고 챕터가 바뀌면 비운다.
// 시간은 둘로 센다 — 물려받은 것(Inherited)과 이 회차에서 새로 플레이한 것(Own).
//
// 갈라지기 (F2): 이력의 장면 기록 하나를 물려받아 새 회차 파일을 쓰고 활성으로 세운다.
// 옛 회차 파일은 그대로 남는다. 그 뒤 런처가 다시 띄우면 재개 경로가 새 회차를 연다.
public sealed class SaveCoordinator : IProgressionReporter
{
    private readonly ISaveStore _localStore;
    private readonly SyncQueue _queue; // 서버에 아직 보내지 못한 변경사항들.(무슨 일이 발생했는 가만 기록.)
    private readonly ServerSyncSaveStore _server;
    private readonly int _slotNo; // 활성 회차를 뜻하는 옛 좌표.

    private float _startedAt = Time.realtimeSinceStartup;
    private int _inheritedSeconds;
    private int _ownSecondsBase;

    private string _playthroughId;
    private ForkOrigin _forkedFrom;

    // 이 회차의 장면 기록 이력. 재개 시 파일에서 읽고, 장면 끝마다 하나씩 붙인다.
    private readonly List<SceneRecord> _scenes = new();

    // 진행 중 장면의 진입 스냅샷. 장면 끝에 경로와 합쳐 기록이 된다.
    private SceneCheckpoint _currentEntry;

    // 서버 사본 쪽. 셋 다 서버가 없으면 null.
    private readonly ServerBookmarkSync _bookmarkSync;
    private readonly ServerRestore _restore;

    // 409로 갈라졌다 — UI가 한 줄 알릴 재료(출처). 사용자가 시키지 않았는데 회차가 둘이 된 경우다.
    public event Action<ForkOrigin> ConflictForked;

    public SaveCoordinator(
        ISaveStore localStore,
        SyncQueue queue,
        ServerSyncSaveStore server,
        int slotNo,
        ServerBookmarkSync bookmarkSync = null,
        ServerRestore restore = null)
    {
        _localStore = localStore;
        _queue = queue;
        _server = server;
        _slotNo = slotNo;
        _bookmarkSync = bookmarkSync;
        _restore = restore;

        if (_server != null)
            _server.ConflictDetected += HandleConflict;
    }

    public IReadOnlyList<SceneRecord> Scenes => _scenes;
    public string PlaythroughId => _playthroughId;

    private int OwnSeconds => _ownSecondsBase + (int)(Time.realtimeSinceStartup - _startedAt);
    private int TotalSeconds => _inheritedSeconds + OwnSeconds;

    // 시작 동기화의 Task. 복구·409 갈라지기가 활성 파일을 쓰는 동안 진행을 시작하면 안 되니,
    // 재개·새 게임은 이것을 먼저 기다린다.
    public Task StartupSync { get; private set; } = Task.CompletedTask;

    public Task SyncPendingAsync() => StartupSync = SyncPendingCoreAsync();

    // 시작 시 서버와 맞춘다: 새 기기면 복구 → 옛 회차 큐 순회 → 즐겨찾기 → 활성 회차. 순서대로 기다린다.
    // 복구는 로컬에 회차가 하나도 없을 때만 — 로컬이 진실이라 있는 것은 덮지 않는다.
    private async Task SyncPendingCoreAsync()
    {
        if (_server == null)
            return;

        // Load가 옛 형식(slot1.json)을 먼저 옮긴다. 그 뒤에도 비어 있으면 새 기기.
        if (_restore != null && _localStore.Load(_slotNo) == null && _localStore.ListPlaythroughIds().Count == 0)
            await _restore.RestoreAsync();

        // 큐를 활성 회차 것으로 맞춘 뒤에 보낸다.
        string activeId = _localStore.ActiveId;

        if (activeId != null)
            _queue.SwitchTo(_localStore.QueuePathOf(activeId));

        await _server.SyncStaleQueuesAsync(_slotNo, _localStore.ListPlaythroughIds(), activeId);

        if (_bookmarkSync != null)
            await _bookmarkSync.SyncAllAsync();

        await _server.TrySyncAsync(_slotNo);
    }

    // Flushes pending sync work, then clears the active pointer and starts a fresh playthrough.
    // 옛 회차 파일은 남는다. The previous server-side playthrough remains open until session-ending support is added.
    public async Task StartNewGameAsync()
    {
        if (_server != null)
            await _server.FlushAsync(_slotNo);

        int dropped = _queue.PendingCount;

        if (dropped > 0)
            Debug.LogWarning($"[저장] 새 게임 - 서버에 못 보낸 이력 {dropped} 남겨 둠(옛 회차 큐). 다음 시작에 다시 보낸다.");

        _localStore.Delete(_slotNo);

        BecomePlaythrough(NewPlaythroughId(), forkedFrom: null, inheritedSeconds: 0, ownSeconds: 0, scenes: null);
        _queue.Reset();

        Debug.Log($"[저장] 새 게임 - 회차 {_playthroughId}");
    }

    public ProgressionResumePoint GetResumePoint()
    {
        LocalSaveFile save = _localStore.Load(_slotNo);

        if (save == null)
            return null;

        // 구세이브(회차 id 없음)는 지금 id를 받는다.
        string id = string.IsNullOrEmpty(save.PlaythroughId) 
            ? NewPlaythroughId() 
            : save.PlaythroughId;

        int playSeconds = save.InheritedPlaySeconds == 0 && save.OwnPlaySeconds == 0
            ? save.PlaySeconds
            : save.OwnPlaySeconds;

        BecomePlaythrough(id, save.ForkedFrom, save.InheritedPlaySeconds, playSeconds, save.Scenes);

        return new ProgressionResumePoint(
            save.ChapterId,
            save.CurrentEpisodeId,
            save.Stats,
            save.Variables,
            save.Backlog,
            save.PendingLoad,
            save.ChapterCompleted);
    }

    // 백로그 항목 → 그 장면 안 라인 좌표. 등장 순번은 그 장면의 백로그 안에서 같은 (노드, 라인)을 센 것 —
    // 롤백 포인트의 occurrence와 같은 좌표계다(둘 다 장면 시작에서 0부터 센다).
    public bool TryMakeLineTarget(in DialogueLogEntry entry, out int sceneIndex, out SaveLineTarget target)
    {
        target = null;
        sceneIndex = FindSceneIndexBySerial(entry.lineSerial);

        if (sceneIndex < 0)
            return false;

        LocalSaveFile current = _localStore.Load(_slotNo);

        if (current?.Backlog == null)
            return false;

        int start = _scenes[sceneIndex].Checkpoint.BacklogSerialStart;
        int occurrence = 0;

        for (int i = 0; i < current.Backlog.Count; i++)
        {
            DialogueLogEntry e = current.Backlog[i];

            if (e.lineSerial < start || e.lineSerial > entry.lineSerial)
                continue;

            if (string.Equals(e.nodeName, entry.nodeName, StringComparison.Ordinal) &&
                string.Equals(e.lineId, entry.lineId, StringComparison.Ordinal))
            {
                occurrence++;
            }
        }

        if (occurrence == 0)
            return false;

        target = new SaveLineTarget { NodeName = entry.nodeName, LineId = entry.lineId, Occurrence = occurrence };
        return true;
    }

    // ── 갈라지기 ─────────────────────────────────────────────────────────────

    // 백로그 순번이 속한 장면 기록. 현재 장면(아직 기록 전)이나 다른 챕터면 -1.
    public int FindSceneIndexBySerial(int lineSerial)
    {
        for (int i = 0; i < _scenes.Count; i++)
        {
            SceneCheckpoint checkpoint = _scenes[i].Checkpoint;

            if (lineSerial >= checkpoint.BacklogSerialStart && lineSerial < _scenes[i].BacklogSerialEnd)
                return i;
        }

        return -1;
    }

    public bool CanForkTo(int lineSerial) => FindSceneIndexBySerial(lineSerial) >= 0;

    // 이력의 장면 기록 하나를 물려받아 새 회차를 쓰고 활성으로 세운다. 호출자는 드라이버를 멈춘 뒤
    // 부르고, 그 뒤 런처를 다시 띄운다 — 재개 경로가 새 회차를 그 장면 루트에서 연다.
    //
    // target이 있으면 그 장면의 경로·Yarn 선택과 함께 로드 계획(PendingLoad)으로 실린다 — 첫 장면이
    // 루트에서 표적까지 달린다. 물려받는 것: 그 장면 앞까지의 기록·백로그·누적 시간. 옛 회차 파일과 큐는 그대로.
    //
    // 갈라지기 전에 옛 회차 큐를 한 번 비워 본다(best-effort) — 부모의 마지막 장면 이력이 서버에 먼저 닿도록.
    // 실패해도 갈라진다. 남은 것은 시작 시 큐 순회가 살린다.
    public async Task ForkFromScene(int sceneIndex, SaveLineTarget target = null)
    {
        if (sceneIndex < 0 || sceneIndex >= _scenes.Count)
            throw new ArgumentOutOfRangeException(nameof(sceneIndex));

        await FlushBeforeForkAsync();

        SceneRecord origin = _scenes[sceneIndex];
        SceneCheckpoint checkpoint = origin.Checkpoint;

        LocalSaveFile current = _localStore.Load(_slotNo);

        var backlog = new List<DialogueLogEntry>();

        if (current?.Backlog != null)
        {
            for (int i = 0; i < current.Backlog.Count; i++)
            {
                if (current.Backlog[i].lineSerial < checkpoint.BacklogSerialStart)
                    backlog.Add(current.Backlog[i]);
            }
        }

        string fromId = _playthroughId;
        string newId = NewPlaythroughId();

        var file = new LocalSaveFile
        {
            SlotNo = _slotNo,
            PlaythroughId = newId,
            ForkedFrom = new ForkOrigin { PlaythroughId = fromId, SceneIndex = sceneIndex, Target = target },
            ChapterId = checkpoint.ChapterId,
            CurrentEpisodeId = checkpoint.EpisodeId,
            Stats = new Dictionary<string, int>(checkpoint.Stats, StringComparer.Ordinal),
            Variables = checkpoint.Variables,
            ChapterCompleted = false,
            Scenes = _scenes.Take(sceneIndex).ToList(),
            Backlog = backlog,
            PendingLoad = target == null
                ? null
                : new SavedLoadPlan
                {
                    Path = origin.Path.Select(c => new SavedChoice { FromEpisodeId = c.FromEpisodeId, OptionIndex = c.OptionIndex }).ToList(),
                    YarnChoices = new List<VNChoiceRecord>(origin.YarnChoices),
                    Target = target,
                },
            InheritedPlaySeconds = checkpoint.PlaySecondsAtEntry,
            OwnPlaySeconds = 0,
            PlaySeconds = checkpoint.PlaySecondsAtEntry,
            SavedAtUtc = NowUtc(),
        };

        _localStore.Save(file);

        _queue.SwitchTo(_localStore.QueuePathOf(newId));
        _queue.Reset();

        Debug.Log(
            $"[저장] 갈라지기 — {fromId} 장면 {sceneIndex}({checkpoint.EpisodeId}) → 새 회차 {newId}. " +
            $"물려받은 기록 {file.Scenes.Count}개, 백로그 {backlog.Count}줄, 시간 {checkpoint.PlaySecondsAtEntry}s" +
            (target == null ? " — 장면 루트에서" : $" — {target.NodeName}/{target.LineId}#{target.Occurrence}까지 달린다"));
    }

    // ── 즐겨찾기 ─────────────────────────────────────────────────────────────

    public IReadOnlyList<Bookmark> Bookmarks => _localStore.LoadBookmarks().Items;

    // 지금 라인을 즐겨찾기로. 스스로 완결된 사본 — 진입 스냅샷·찍은 순간까지의 경로·Yarn 선택·표적·이전 백로그.
    // 장면 밖(진입 보고 전)이면 null.
    public Bookmark CreateBookmark(
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target,
        string preview,
        string label = null)
    {
        if (_currentEntry == null || target == null)
            return null;

        LocalSaveFile current = _localStore.Load(_slotNo);

        var bookmark = new Bookmark
        {
            Id = NewPlaythroughId(),
            Label = string.IsNullOrEmpty(label) ? preview : label,
            Preview = preview,
            CreatedAtUtc = NowUtc(),
            PlaythroughId = _playthroughId,
            SceneIndex = _scenes.Count,
            ChapterId = _currentEntry.ChapterId,
            Checkpoint = _currentEntry,
            Load = new SavedLoadPlan
            {
                Path = path.Select(c => new SavedChoice { FromEpisodeId = c.FromEpisodeId, OptionIndex = c.OptionIndex }).ToList(),
                YarnChoices = new List<VNChoiceRecord>(yarnChoices),
                Target = target,
            },
            Backlog = current?.Backlog != null ? new List<DialogueLogEntry>(current.Backlog) : new List<DialogueLogEntry>(),
            PlaySecondsAtBookmark = TotalSeconds,
        };

        BookmarkFile file = _localStore.LoadBookmarks();
        file.Items.Add(bookmark);
        _localStore.SaveBookmarks(file);

        Debug.Log(
            $"[저장] 즐겨찾기 — \"{bookmark.Preview}\" @ {target.NodeName}/{target.LineId}#{target.Occurrence}, " +
            $"경로 {bookmark.Load.Path.Count}개, Yarn 선택 {bookmark.Load.YarnChoices.Count}개 (총 {file.Items.Count}개)");

        // 서버엔 직접 PUT — 큐 없이. 실패하면 SyncedAtUtc가 비어 있어 다음 시작에 다시.
        if (_bookmarkSync != null)
            _ = _bookmarkSync.PushAsync(bookmark.Id);

        return bookmark;
    }

    // 로컬에서 빼고 서버 DELETE. 못 지우면 PendingDeletes에 남아 다음 시작에 다시.
    public bool DeleteBookmark(string id)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        int removed = file.Items.RemoveAll(b => string.Equals(b.Id, id, StringComparison.Ordinal));

        if (removed == 0)
            return false;

        if (_bookmarkSync != null && !file.PendingDeletes.Contains(id))
            file.PendingDeletes.Add(id);

        _localStore.SaveBookmarks(file);

        if (_bookmarkSync != null)
            _ = _bookmarkSync.DeleteAsync(id);

        return true;
    }

    // 이름이 바뀌면 서버 사본도 바뀌어야 한다 — 같은 id로 다시 PUT(멱등 upsert).
    public bool RenameBookmark(string id, string label)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark bookmark = file.Items.Find(b => string.Equals(b.Id, id, StringComparison.Ordinal));

        if (bookmark == null)
            return false;

        bookmark.Label = string.IsNullOrEmpty(label) ? bookmark.Preview : label;
        bookmark.SyncedAtUtc = null;
        _localStore.SaveBookmarks(file);

        if (_bookmarkSync != null)
            _ = _bookmarkSync.PushAsync(id);

        return true;
    }

    // ── 회차 목록 (이력 화면 재료) ────────────────────────────────────────────

    // 보관 중인 회차 요약. 활성 회차와 즐겨찾기가 걸린 회차를 펼치고 나머지는 접는 것은 UI의 일 —
    // 여기서는 그 판단에 필요한 것만 준다.
    public IReadOnlyList<PlaythroughSummary> ListPlaythroughs()
    {
        var summaries = new List<PlaythroughSummary>();

        string activeId = _localStore.ActiveId;
        BookmarkFile bookmarks = _localStore.LoadBookmarks();

        foreach (string id in _localStore.ListPlaythroughIds())
        {
            LocalSaveFile file = _localStore.LoadPlaythrough(id);

            if (file == null)
                continue;

            summaries.Add(new PlaythroughSummary
            {
                PlaythroughId = id,
                IsActive = string.Equals(id, activeId, StringComparison.Ordinal),
                ForkedFrom = file.ForkedFrom,
                ChapterId = file.ChapterId,
                CurrentEpisodeId = file.CurrentEpisodeId,
                ChapterCompleted = file.ChapterCompleted,
                SceneCount = file.Scenes?.Count ?? 0,
                BookmarkCount = bookmarks.Items.Count(b => string.Equals(b.PlaythroughId, id, StringComparison.Ordinal)),
                InheritedPlaySeconds = file.InheritedPlaySeconds,
                OwnPlaySeconds = file.OwnPlaySeconds,
                SavedAtUtc = file.SavedAtUtc,
            });
        }

        summaries.Sort((a, b) => string.CompareOrdinal(b.SavedAtUtc, a.SavedAtUtc));

        return summaries;
    }

    // 즐겨찾기를 물려받아 새 회차로. 출처 회차 파일이 있으면 앞의 장면 기록도 물려받는다.
    // 호출자는 드라이버를 멈춘 뒤 부르고, 그 뒤 런처를 다시 띄운다.
    public async Task ForkFromBookmark(Bookmark bookmark)
    {
        await FlushBeforeForkAsync();

        SceneCheckpoint checkpoint = bookmark.Checkpoint;

        LocalSaveFile origin = string.IsNullOrEmpty(bookmark.PlaythroughId)
            ? null
            : _localStore.LoadPlaythrough(bookmark.PlaythroughId);

        List<SceneRecord> inherited = origin?.Scenes != null
            ? origin.Scenes.Take(Math.Min(bookmark.SceneIndex, origin.Scenes.Count)).ToList()
            : new List<SceneRecord>();

        string newId = NewPlaythroughId();

        var file = new LocalSaveFile
        {
            SlotNo = _slotNo,
            PlaythroughId = newId,
            ForkedFrom = new ForkOrigin
            {
                PlaythroughId = bookmark.PlaythroughId,
                SceneIndex = bookmark.SceneIndex,
                Target = bookmark.Load?.Target,
            },
            ChapterId = checkpoint.ChapterId,
            CurrentEpisodeId = checkpoint.EpisodeId,
            Stats = new Dictionary<string, int>(checkpoint.Stats, StringComparer.Ordinal),
            Variables = checkpoint.Variables,
            ChapterCompleted = false,
            Scenes = inherited,
            Backlog = new List<DialogueLogEntry>(bookmark.Backlog),
            PendingLoad = bookmark.Load,
            InheritedPlaySeconds = bookmark.PlaySecondsAtBookmark,
            OwnPlaySeconds = 0,
            PlaySeconds = bookmark.PlaySecondsAtBookmark,
            SavedAtUtc = NowUtc(),
        };

        _localStore.Save(file);

        _queue.SwitchTo(_localStore.QueuePathOf(newId));
        _queue.Reset();

        Debug.Log(
            $"[저장] 즐겨찾기로 갈라지기 — \"{bookmark.Preview}\" → 새 회차 {newId}. " +
            $"물려받은 기록 {inherited.Count}개, 백로그 {file.Backlog.Count}줄, 시간 {bookmark.PlaySecondsAtBookmark}s");
    }

    // ── 보고 ────────────────────────────────────────────────────────────────

    // 장면에 들어섰다 — 진입 스냅샷을 받아 둔다. 이력은 현재 챕터 안에서만.
    public void ReportSceneEntered(SceneEntryReport report)
    {
        if (_scenes.Count > 0 &&
            !string.Equals(_scenes[^1].Checkpoint.ChapterId, report.ChapterId, StringComparison.Ordinal))
        {
            _scenes.Clear();
        }

        _currentEntry = new SceneCheckpoint
        {
            ChapterId = report.ChapterId,
            EpisodeId = report.State.CurrentEpisodeId,
            Stats = report.State.Stats.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            Variables = report.Variables,
            BacklogSerialStart = report.BacklogSerialStart,
            LastChoiceSeq = _queue.NextSeq - 1,
            PlaySecondsAtEntry = TotalSeconds,
            EnteredAtUtc = NowUtc(),
        };
    }

    // 장면이 끝나 확정됐을 때 호출 — 장면 기록 완성 -> 로컬 저장 1회 -> 큐 적재(순서대로) -> 동기화 시도.
    public void ReportSceneCommitted(SceneCommitReport report)
    {
        string now = NowUtc();

        if (_playthroughId == null)
            BecomePlaythrough(NewPlaythroughId(), null, 0, 0, null);

        // 진입 보고 없이 fold가 왔다면(있어서는 안 된다) 기록 없이 상태만 저장한다.
        if (_currentEntry != null)
        {
            var path = new List<SavedChoice>(report.Choices.Count);

            for (int i = 0; i < report.Choices.Count; i++)
                path.Add(new SavedChoice
                {
                    FromEpisodeId = report.Choices[i].FromEpisodeId,
                    OptionIndex = report.Choices[i].OptionIndex,
                });

            _scenes.Add(new SceneRecord
            {
                Checkpoint = _currentEntry,
                Path = path,
                YarnChoices = new List<VNChoiceRecord>(report.YarnChoices),
                BacklogSerialEnd = report.BacklogSerialStart,
            });

            _currentEntry = null;
        }
        else
        {
            Debug.LogWarning("[저장] 진입 보고 없이 장면이 끝났다 — 장면 기록 없이 상태만 저장한다.");
        }

        int own = OwnSeconds;

        _localStore.Save(new LocalSaveFile
        {
            SlotNo = _slotNo,
            PlaythroughId = _playthroughId,
            ForkedFrom = _forkedFrom,
            ChapterId = report.ChapterId,
            CurrentEpisodeId = report.State.CurrentEpisodeId,
            Stats = report.State.Stats.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            Variables = report.Variables,
            ChapterCompleted = report.ChapterCompleted,
            Scenes = new List<SceneRecord>(_scenes),
            Backlog = new List<DialogueLogEntry>(report.Backlog),
            InheritedPlaySeconds = _inheritedSeconds,
            OwnPlaySeconds = own,
            PlaySeconds = _inheritedSeconds + own,
            SavedAtUtc = now,
        });

        for (int i = 0; i < report.Choices.Count; i++)
            _queue.EnqueueChoice(report.Choices[i].FromEpisodeId, report.Choices[i].OptionIndex, now);

        // Repeated visits are deduplicated server-side via the episode's EventKey.
        for (int i = 0; i < report.WatchedEpisodeIds.Count; i++)
            _queue.EnqueueEvent(report.WatchedEpisodeIds[i], now);

        Debug.Log(
            $"[저장] 장면 확정 — 선택 {report.Choices.Count}, Yarn 선택 {report.YarnChoices.Count}, " +
            $"시청 {report.WatchedEpisodeIds.Count}, [3] {report.Variables?.Count ?? 0}개, 백로그 {report.Backlog.Count}줄, " +
            $"기록 {_scenes.Count}개, 시간 {_inheritedSeconds}+{own}s → {report.State.CurrentEpisodeId}" +
            (report.ChapterCompleted ? " (챕터 완료)" : string.Empty));

        if (_server != null)
            _ = _server.TrySyncAsync(_slotNo);
    }

    // ── 409 ─────────────────────────────────────────────────────────────────

    // 다른 기기가 이 회차를 먼저 저장했다. 확정된 것은 되돌리지 않는다 — 이 기기의 진행을 새 회차로 갈라 이어 간다.
    // 미전송 선택·이벤트는 새 회차 큐의 seq 1..n으로 다시 매기고, 출처 장면은 서버가 마지막으로 받아 준 자리.
    // 재생·이력·진입 스냅샷은 손대지 않는다. 옛 회차 파일은 남고(서버와 같은 지점까지의 기록), 활성만 새 회차로.
    // 옛 큐는 미전송을 넘겼으니 비운다 — 시작 시 순회가 같은 409를 또 맞지 않게. force(덮어쓰기)는 노출하지 않는다.
    private void HandleConflict()
    {
        LocalSaveFile current = _localStore.Load(_slotNo);

        if (current == null)
            return;

        // 메모리가 회차를 알고 있는데 파일과 다르면 활성이 그 사이 바뀐 것 — 이번 충돌은 지나간 회차의 것.
        if (_playthroughId != null && !string.Equals(_playthroughId, current.PlaythroughId, StringComparison.Ordinal))
            return;

        SyncBatch pending = _queue.CaptureBatch();
        int sceneIndex = _queue.SyncedSceneCount;
        string fromId = current.PlaythroughId;
        string newId = NewPlaythroughId();

        var origin = new ForkOrigin { PlaythroughId = fromId, SceneIndex = sceneIndex, Target = null };

        current.PlaythroughId = newId;
        current.ForkedFrom = origin;
        current.SavedAtUtc = NowUtc();

        // 시간도 다른 갈라지기처럼 나눈다 — 출처 장면 진입까지가 물려받은 것, 나머지가 이 회차 것.
        if (current.Scenes != null && sceneIndex < current.Scenes.Count)
        {
            current.InheritedPlaySeconds = current.Scenes[sceneIndex].Checkpoint.PlaySecondsAtEntry;
            current.OwnPlaySeconds = Math.Max(0, current.PlaySeconds - current.InheritedPlaySeconds);
        }

        _localStore.Save(current);

        _queue.Discard(pending);
        _queue.SwitchTo(_localStore.QueuePathOf(newId));
        _queue.Reset(pending.Choices, pending.Events);

        // 재개 전(시작 시 동기화)이면 메모리는 비어 있다 — 재개가 새 파일을 읽으며 채운다.
        if (_playthroughId != null)
        {
            _playthroughId = newId;
            _forkedFrom = origin;
            _inheritedSeconds = current.InheritedPlaySeconds;
            _ownSecondsBase = current.OwnPlaySeconds;
            _startedAt = Time.realtimeSinceStartup;
        }

        Debug.LogWarning(
            $"[저장] 충돌(409) — 다른 기기가 회차 {fromId}를 먼저 저장했다. 이 기기의 진행은 새 회차 {newId}로 갈라 이어 간다 " +
            $"(출처 장면 {sceneIndex}, 미전송 선택 {pending.Choices.Count}건 → seq 1부터, 이벤트 {pending.Events.Count}건).");

        ConflictForked?.Invoke(origin);

        _ = _server.TrySyncAsync(_slotNo);
    }

    // ── 잔손 ────────────────────────────────────────────────────────────────

    private async Task FlushBeforeForkAsync()
    {
        if (_server == null)
            return;

        await _server.FlushAsync(_slotNo);

        int left = _queue.PendingCount;

        if (left > 0)
            Debug.LogWarning($"[저장] 갈라지기 전 동기화 못 함 — 옛 회차 큐에 {left}건 남김. 다음 시작에 다시 보낸다.");
    }

    // 메모리를 이 회차의 것으로. 큐도 그 회차의 파일로 옮겨 탄다.
    private void BecomePlaythrough(
        string id, ForkOrigin forkedFrom, int inheritedSeconds, int ownSeconds, List<SceneRecord> scenes)
    {
        _playthroughId = id;
        _forkedFrom = forkedFrom;
        _inheritedSeconds = inheritedSeconds;
        _ownSecondsBase = ownSeconds;
        _startedAt = Time.realtimeSinceStartup;

        _scenes.Clear();

        if (scenes != null)
            _scenes.AddRange(scenes);

        _currentEntry = null;

        _queue.SwitchTo(_localStore.QueuePathOf(id));
    }

    private static string NewPlaythroughId() => Guid.NewGuid().ToString("N");

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
}
