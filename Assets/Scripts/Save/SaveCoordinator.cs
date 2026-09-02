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
public sealed class SaveCoordinator : IProgressionReporter
{
    private readonly ISaveStore _localStore;
    private readonly SyncQueue _queue; // 서버에 아직 보내지 못한 변경사항들.(무슨 일이 발생했는 가만 기록.)
    private readonly ServerSyncSaveStore _server;
    private readonly int _slotNo; // 몇 번째 세이브 슬롯인지.

    private float _startedAt = Time.realtimeSinceStartup;
    private int _inheritedSeconds;
    private int _ownSecondsBase;

    private string _playthroughId;
    private ForkOrigin _forkedFrom;

    // 이 회차의 장면 기록 이력. 재개 시 파일에서 읽고, 장면 끝마다 하나씩 붙인다.
    private readonly List<SceneRecord> _scenes = new();

    // 진행 중 장면의 진입 스냅샷. 장면 끝에 경로와 합쳐 기록이 된다.
    private SceneCheckpoint _currentEntry;

    public SaveCoordinator(
        ISaveStore localStore,
        SyncQueue queue,
        ServerSyncSaveStore server,
        int slotNo)
    {
        _localStore = localStore;
        _queue = queue;
        _server = server;
        _slotNo = slotNo;
    }

    public IReadOnlyList<SceneRecord> Scenes => _scenes;

    private int OwnSeconds => _ownSecondsBase + (int)(Time.realtimeSinceStartup - _startedAt);
    private int TotalSeconds => _inheritedSeconds + OwnSeconds;

    // Syncs any pending items left from the previous session on startup.
    public Task SyncPendingAsync() =>
        _server == null
            ? Task.CompletedTask
            : _server.TrySyncAsync(_slotNo);

    // Flushes pending sync work, then clears the local save and queue for a new game.
    // The previous server-side playthrough remains open until session-ending support is added.
    public async Task StartNewGameAsync()
    {
        if (_server != null)
            await _server.FlushAsync(_slotNo);

        int dropped = _queue.PendingCount;

        if (dropped > 0)
            Debug.LogWarning($"[저장] 새 게임 - 서버에 못 보낸 이력 {dropped} 버림.");

        _queue.Reset();
        _localStore.Delete(_slotNo);

        _playthroughId = NewPlaythroughId();
        _forkedFrom = null;
        _inheritedSeconds = 0;
        _ownSecondsBase = 0;
        _startedAt = Time.realtimeSinceStartup;
        _scenes.Clear();
        _currentEntry = null;

        Debug.Log($"[저장] 새 게임 - 세이브/큐 초기화. 회차 {_playthroughId}");
    }

    public ProgressionResumePoint GetResumePoint()
    {
        LocalSaveFile save = _localStore.Load(_slotNo);

        if (save == null)
            return null;

        // 구세이브(회차 id 없음)는 지금 id를 받는다. 시간이 한 칸뿐이면 전부 자체 시간으로 본다.
        _playthroughId = string.IsNullOrEmpty(save.PlaythroughId) ? NewPlaythroughId() : save.PlaythroughId;
        _forkedFrom = save.ForkedFrom;
        _inheritedSeconds = save.InheritedPlaySeconds;
        _ownSecondsBase = save.InheritedPlaySeconds == 0 && save.OwnPlaySeconds == 0
            ? save.PlaySeconds
            : save.OwnPlaySeconds;
        _startedAt = Time.realtimeSinceStartup;

        _scenes.Clear();

        if (save.Scenes != null)
            _scenes.AddRange(save.Scenes);

        _currentEntry = null;

        return new ProgressionResumePoint(
            save.ChapterId,
            save.CurrentEpisodeId,
            save.Stats,
            save.Variables,
            save.Backlog,
            save.ChapterCompleted);
    }

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
            _playthroughId = NewPlaythroughId();

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

    private static string NewPlaythroughId() => Guid.NewGuid().ToString("N");

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
}
