using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Bridges progression reports into the save layer.
// This is the save layer's only dependency on progression-specific types.
//
// 1) 현재 상태를 먼저 보존    / localStore.Save(...); 
// 2) 서버용 이력을 남김       / _queue.EnqueueChoice(...);
// 3) 서버 전송은 기다리지 않음 / _server.TrySyncAsync(...);
public sealed class SaveCoordinator : IProgressionReporter
{
    private readonly ISaveStore _localStore;
    private readonly SyncQueue _queue; // 서버에 아직 보내지 못한 변경사항들.(무슨 일이 발생했는 가만 기록.)
    private readonly ServerSyncSaveStore _server;
    private readonly int _slotNo; // 몇 번째 세이브 슬롯인지.

    private readonly float _startedAt = Time.realtimeSinceStartup;
    private int _basePlaySeconds;

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
        _basePlaySeconds = 0;

        Debug.Log("[저장] 새 게임 - 세이브/큐 초기화.");
    }

    public ProgressionResumePoint GetResumePoint()
    {
        LocalSaveFile save = _localStore.Load(_slotNo);

        if (save == null)
            return null;

        _basePlaySeconds = save.PlaySeconds;

        return new ProgressionResumePoint(
            save.ChapterId, 
            save.CurrentEpisodeId,
            save.Stats);
    }

    // 게임에서 선택 하나가 확정됐을 때 호출.
    // 로컬 저장 -> 큐 적재 -> 동기화 시도.
    public void ReportChoiceCommitted(ChoiceCommitReport report)
    {
        string now = NowUtc();

        // 현재 게임 상태 전체를 스냅샷으로 저장.
        // report.NewState 사용.(선택이 이미 확정된 후의 결과 상태)
        _localStore.Save(new LocalSaveFile
        {
            SlotNo = _slotNo,
            ChapterId = report.ChapterId,
            CurrentEpisodeId = report.NewState.CurrentEpisodeId,
            Stats = report.NewState.Stats.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            PlaySeconds = _basePlaySeconds + (int)(Time.realtimeSinceStartup - _startedAt),
            SavedAtUtc = now,
        });

        _queue.EnqueueChoice(report.FromEpisodeId, report.OptionIndex, now);

        if (_server != null)
            _ = _server.TrySyncAsync(_slotNo);
    }

    public void ReportEpisodeWatched(EpisodeWatchReport report)
    {
        // Records history only. progression state is unchanged.
        // Repeated visits are deduplicated server-side via the episode's EventKey.
        _queue.EnqueueEvent(report.EpisodeId, NowUtc());

        if (_server != null)
            _ = _server.TrySyncAsync(_slotNo);
    }

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
}