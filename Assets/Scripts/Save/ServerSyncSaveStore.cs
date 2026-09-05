using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Syncs locally persisted changes to the server.
//
// The local save is authoritative. This class only uploads a server-side copy.
// Failed sync attempts leave the queue untouched so they can be retried later.
//
// One sync attempt (per playthrough file + its queue):
// [1] Resolve or create the server-side playthrough (idempotent on the local guid).
// [2] Resolve the chapter version.
// [3] Capture the current pending queue as a batch.
// [4] Upload the batch together with the latest local save snapshot.
// [5] Acknowledge and remove the batch only after a successful response.
//
// Any failure simply ends the current attempt. Offline play is an expected path.
//
// 활성 회차는 TrySyncAsync(코디네이터가 쥔 큐 인스턴스), 옛 회차들은 SyncStaleQueuesAsync(파일마다 큐를 새로 연다).
public sealed class ServerSyncSaveStore
{
    private enum SyncOutcome
    {
        Done,
        Failed,
        Conflict,
    }

    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly SyncQueue _queue;
    private readonly ChapterVersionResolver _versionResolver;
    private readonly ISaveStore _localStore;
    private readonly string _deviceKey;

    // Only one active sync may run at a time.
    // Requests arriving while one is in flight are coalesced into one follow-up sync.
    private Task _inFlight;
    private bool _syncAgain;

    // 409 — 다른 기기가 활성 회차를 먼저 저장했다. 큐는 손대지 않은 채 넘긴다. 해소(갈라지기)는 코디네이터.
    public event Action ConflictDetected;

    public ServerSyncSaveStore(
        ServerApi api,
        GuestSession session,
        SyncQueue queue,
        ChapterVersionResolver versionResolver,
        ISaveStore localStore,
        string deviceKey)
    {
        _api = api;
        _session = session;
        _queue = queue;
        _versionResolver = versionResolver;
        _localStore = localStore;
        _deviceKey = deviceKey;
    }

    // Starts a sync attempt if none is running.
    // If a sync is already in progress,
    // schedules one follow-up attempt and returns the current task.
    public Task TrySyncAsync(int slotNo)
    {
        if (_inFlight != null)
        {
            _syncAgain = true;
            return _inFlight;
        }

        Task run = RunAsync(slotNo);

        // Avoid marking an already-completed sync as in flight.
        if (!run.IsCompleted)
            _inFlight = run;

        return run;
    }

    // Waits for the current sync and any queued follow-up sync to finish.
    // Intended for transition points such as starting a new game or forking,
    // where no new commits are expected.
    public async Task FlushAsync(int slotNo)
    {
        if (_inFlight == null)
            await TrySyncAsync(slotNo);

        while (_inFlight != null)
            await _inFlight;
    }

    // 옛 회차 큐에 남은 미전송. 활성 회차는 건너뛴다 — 같은 큐 파일을 두 인스턴스가 쓰지 않도록.
    // 순서는 맞추지 않는다. 자식이 부모보다 먼저 가도 서버가 나중에 잇는다.
    public async Task SyncStaleQueuesAsync(int slotNo, IReadOnlyList<string> playthroughIds, string activeId)
    {
        for (int i = 0; i < playthroughIds.Count; i++)
        {
            string id = playthroughIds[i];

            if (string.Equals(id, activeId, StringComparison.Ordinal))
                continue;

            var queue = new SyncQueue(_localStore.QueuePathOf(id));

            if (queue.PendingCount == 0 || queue.ConflictedAtUtc != null)
                continue;

            LocalSaveFile save = _localStore.LoadPlaythrough(id);

            if (save == null)
                continue;

            Debug.Log($"[동기화] 옛 회차 {id} — 미전송 {queue.PendingCount}건을 보낸다.");

            try
            {
                SyncOutcome outcome = await SyncOnceAsync(save, queue, slotNo);

                // 옛 회차의 409는 갈라지지 않는다 — 활성이 아니라 이어 갈 진행이 없다. 표시해 두고 다음부터 건너뛴다.
                if (outcome == SyncOutcome.Conflict)
                {
                    queue.MarkConflicted(DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
                    Debug.LogWarning($"[동기화] 옛 회차 {id} 충돌(409) — 다른 기기가 앞섰다. 큐 보존, 다시 보내지 않는다.");
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"[동기화] 옛 회차 {id} 실패\n{error}");
            }
        }
    }

    private async Task RunAsync(int slotNo)
    {
        try
        {
            await SyncActiveAsync(slotNo);
        }
        catch (Exception error)
        {
            Debug.LogError($"[동기화] 실패\n{error}");
        }
        finally
        {
            _inFlight = null;

            if (_syncAgain)
            {
                _syncAgain = false;
                _ = TrySyncAsync(slotNo);
            }
        }
    }

    private async Task SyncActiveAsync(int slotNo)
    {
        LocalSaveFile save = _localStore.Load(slotNo);

        // Queue entries are added only after the local snapshot is saved.
        // Without a local save, there is no valid state to upload.
        if (save == null)
            return;

        SyncOutcome outcome = await SyncOnceAsync(save, _queue, slotNo);

        if (outcome == SyncOutcome.Conflict)
            ConflictDetected?.Invoke();
    }

    private async Task<SyncOutcome> SyncOnceAsync(LocalSaveFile save, SyncQueue queue, int slotNo)
    {
        long? playthroughId = queue.PlaythroughId ?? await CreatePlaythroughAsync(save, queue);

        if (playthroughId == null)
            return SyncOutcome.Failed;

        int? chapterVersion = await _versionResolver.ResolveAsync(save.ChapterId);

        if (chapterVersion == null)
            return SyncOutcome.Failed;

        SyncBatch batch = queue.CaptureBatch();

        var request = new SaveUploadRequestDto
        {
            ChapterId = save.ChapterId,
            ChapterVersion = chapterVersion.Value,
            CurrentEpisodeId = save.CurrentEpisodeId,
            Snapshot = save,
            PlaySeconds = save.PlaySeconds,
            DeviceKey = _deviceKey,
            BaseRevision = queue.BaseRevision ?? 0,
            Choices = batch.Choices,
            Events = batch.Events,
            InheritedPlaySeconds = save.InheritedPlaySeconds,
            OwnPlaySeconds = save.OwnPlaySeconds,
            ChapterCompleted = save.ChapterCompleted,
        };

        ApiResult<SaveUploadResponseDto> result =
            await _session.CallAsync(token => _api.PutSaveAsync(playthroughId.Value, slotNo, request, token));

        if (result.Ok)
        {
            queue.Acknowledge(batch, result.Body.Revision, save.Scenes?.Count ?? 0);

            Debug.Log($"[동기화] 완료 — revision {result.Body.Revision}, "
                      + $"선택 {batch.Choices.Count}건, 이벤트 {batch.Events.Count}건"
                      + (result.Body.Replayed ? " (재전송 흡수)" : ""));

            return SyncOutcome.Done;
        }

        if (result.ErrorCode == "CONFLICT")
            return SyncOutcome.Conflict;

        // 413은 "줄여 보내라"다. 백로그 상한(300줄) 안이면 닿지 않는다 — 닿았다면 상한이 깨진 것.
        if (result.Status == 413)
            Debug.LogError($"[동기화] 스냅샷이 서버 상한을 넘었다(413) — {result.RawBody}");
        else if (!result.NetworkError)
            Debug.LogWarning($"[동기화] 실패 — HTTP {result.Status} {result.ErrorCode}. 큐 보존.");

        return SyncOutcome.Failed;
    }

    // 회차 파일의 로컬 guid가 멱등 키다 — 같은 guid로 다시 보내면 서버는 있던 회차를 돌려준다(200).
    // 갈래면 부모의 로컬 guid와 장면 번호를 함께. 부모가 아직 서버에 없어도 서버가 나중에 잇는다.
    private async Task<long?> CreatePlaythroughAsync(LocalSaveFile save, SyncQueue queue)
    {
        var request = new PlaythroughCreateRequestDto
        {
            ClientPlaythroughId = save.PlaythroughId,
            ForkedFrom = save.ForkedFrom == null
                ? null
                : new ForkOriginDto
                {
                    ClientPlaythroughId = save.ForkedFrom.PlaythroughId,
                    SceneIndex = save.ForkedFrom.SceneIndex,
                },
        };

        ApiResult<PlaythroughCreatedDto> result =
            await _session.CallAsync(token => _api.CreatePlaythroughAsync(_session.UserId.Value, request, token));

        if (!result.Ok)
        {
            if (!result.NetworkError)
                Debug.LogWarning($"[동기화] 회차 생성 실패 — HTTP {result.Status} {result.ErrorCode}");

            return null;
        }

        queue.SetPlaythroughId(result.Body.PlaythroughId);

        Debug.Log(
            $"[동기화] 회차 {(result.Status == 201 ? "생성" : "확인")}({result.Status}) — playthroughId {result.Body.PlaythroughId}, " +
            $"client {result.Body.ClientPlaythroughId}" +
            (request.ForkedFrom == null ? "" : $", 갈래 ← {request.ForkedFrom.ClientPlaythroughId} 장면 {request.ForkedFrom.SceneIndex}"));

        return result.Body.PlaythroughId;
    }
}
