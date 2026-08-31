using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// 드라이버의 보고를 받아 적는 자리 (M7). 저장 층에서 유일하게 진행 층을 아는 클래스.
//
// 커밋 한 건 = 로컬 저장 → 큐 적재 → 동기화 시동, 이 순서. 로컬이 끝난 뒤에야 서버가 나서고,
// 서버는 실패해도 로컬이 이미 진실이다. 동기화는 기다리지 않는다 — 진행 루프가 저장을 기다릴 이유가 없다.
public sealed class SaveCoordinator : IProgressionReporter
{
    private readonly ISaveStore _localStore;
    private readonly SyncQueue _queue;
    private readonly ServerSyncSaveStore _server;   // null이면 로컬 저장·큐 적재만 한다
    private readonly int _slotNo;

    // playSeconds = 저장돼 있던 값 + 이번 실행에서 흐른 시간. 통계용이라 정밀 계측은 하지 않는다.
    private readonly float _startedAt = Time.realtimeSinceStartup;
    private int _basePlaySeconds;

    public SaveCoordinator(ISaveStore localStore, SyncQueue queue, ServerSyncSaveStore server, int slotNo)
    {
        _localStore = localStore;
        _queue = queue;
        _server = server;
        _slotNo = slotNo;
    }

    // 런처의 resumeProvider. 세이브가 없으면 null(새 게임).
    public ProgressionResumePoint GetResumePoint()
    {
        LocalSaveFile save = _localStore.Load(_slotNo);

        if (save == null)
            return null;

        _basePlaySeconds = save.PlaySeconds;

        return new ProgressionResumePoint(save.ChapterId, save.CurrentEpisodeId, save.Stats);
    }

    // 앱 시작 시 한 번 — 지난 실행이 남긴 큐를 민다.
    public Task SyncPendingAsync() =>
        _server == null ? Task.CompletedTask : _server.TrySyncAsync(_slotNo);

    // 새 게임 . 밀 수 있는 건 밀고, 로컬을 비운다. 다음 커밋의 동기화가 서버에 새 회차를 만듬.
    // 아직 이전 회차는 서버에 열린 채 남는다 (회차 종료 API 는 타이틀 UI 만들 때 같이.)
    public async Task StartNewGameAsync()
    {
        if (_server != null)
            await _server.FlushAsync(_slotNo);

        int dropped = _queue.PendingCount;

        if (dropped > 0)
            Debug.LogWarning($"[저장] 새 게임 — 서버에 못 보낸 이력 {dropped}건을 버린다.");

        _queue.Reset();
        _localStore.Delete(_slotNo);
        _basePlaySeconds = 0;

        Debug.Log("[저장] 새 게임 — 세이브·큐 초기화.");
    }

    public void ReportChoiceCommitted(ChoiceCommitReport report)
    {
        string now = NowUtc();

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

        Sync();
    }

    public void ReportEpisodeWatched(EpisodeWatchReport report)
    {
        // 상태는 안 변했다 — 이력만. 서버가 episodeId로 EventKey를 찾고 재도달은 흡수한다(D-011).
        _queue.EnqueueEvent(report.EpisodeId, NowUtc());

        Sync();
    }

    private void Sync()
    {
        if (_server != null)
            _ = _server.TrySyncAsync(_slotNo);
    }

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
}
