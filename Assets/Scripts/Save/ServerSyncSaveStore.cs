using System;
using System.Threading.Tasks;
using UnityEngine;

// Syncs locally persisted changes to the server.
//
// The local save is authoritative. This class only uploads a server-side copy.
// Failed sync attempts leave the queue untouched so they can be retried later.
//
// One sync attempt:
// [1] Acquire an authentication token.
// [2] Resolve or create the server-side playthrough.
// [3] Resolve the chapter version.
// [4] Capture the current pending queue as a batch.
// [5] Upload the batch together with the latest local save snapshot.
// [6] Acknowledge and remove the batch only after a successful response.
//
// Any failure simply ends the current attempt. Offline play is an expected path.
public sealed class ServerSyncSaveStore
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly SyncQueue _queue;
    private readonly ChapterVersionResolver _versionResolver;
    private readonly ISaveStore _localStore;
    private readonly string _deviceKey;

    
    // Only one sync may run at a time.
    // Requests arriving while one is in flight are coalesced into one follow-up sync.
    private Task _inFlight;
    private bool _syncAgain;
    
    // '409' - 다른 기기가 먼저 저장함. 알리기만 하고 큐 보존.
    // (해소(폐기 vs force)는 차후 진행.)
    public event Action<string> ConflictDetected;

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
    // Intended for transition points such as starting a new game,
    // where no new commits are expected.
    public async Task FlushAsync(int slotNo)
    {
        if (_inFlight == null)
            await TrySyncAsync(slotNo);

        while (_inFlight != null)
            await _inFlight;
    }

    private async Task RunAsync(int slotNo)
    {
        try
        {
            await SyncOnceAsync(slotNo);
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

    private async Task SyncOnceAsync(int slotNo)
    {
        LocalSaveFile save = _localStore.Load(slotNo);

        // Queue entries are added only after the local snapshot is saved.
        // Without a local save, there is no valid state to upload.
        if (save == null)
            return;

        string token = await _session.EnsureTokenAsync();
        if (token == null)
            return;

        long? playthroughId = _queue.PlaythroughId ?? await CreatePlaythroughAsync(token);
        if (playthroughId == null)
            return;

        int? chapterVersion = await _versionResolver.ResolveAsync(save.ChapterId);
        if (chapterVersion == null)
            return;

        SyncBatch batch = _queue.CaptureBatch();

        var request = new SaveUploadRequestDto
        {
            ChapterId = save.ChapterId,
            ChapterVersion = chapterVersion.Value,
            CurrentEpisodeId = save.CurrentEpisodeId,
            Snapshot = save,
            PlaySeconds = save.PlaySeconds,
            DeviceKey = _deviceKey,
            BaseRevision = _queue.BaseRevision ?? 0,
            Choices = batch.Choices,
            Events = batch.Events,
        };

        ApiResult<SaveUploadResponseDto> result =
            await _api.PutSaveAsync(playthroughId.Value, slotNo, request, token);

        // A 401 may indicate that the cached token is no longer valid,
        // for example after a server restart. Refresh it once and retry.
        if (result.Status == 401)
        {
            _session.InvalidateToken();
            token = await _session.EnsureTokenAsync();

            if (token == null)
                return;

            result = await _api.PutSaveAsync(playthroughId.Value, slotNo, request, token);
        }

        if (result.Ok)
        {
            _queue.Acknowledge(batch, result.Body.Revision);

            Debug.Log($"[동기화] 완료 — revision {result.Body.Revision}, "
                      + $"선택 {batch.Choices.Count}건, 이벤트 {batch.Events.Count}건"
                      + (result.Body.Replayed ? " (재전송 흡수)" : ""));

            return;
        }

        if (result.ErrorCode == "CONFLICT")
        {
            Debug.LogWarning("[동기화] 충돌(409) — 다른 기기가 먼저 저장했다. 큐 보존, 해소는 M8.");
            ConflictDetected?.Invoke(result.RawBody);

            return;
        }

        if (!result.NetworkError)
            Debug.LogWarning($"[동기화] 실패 — HTTP {result.Status} {result.ErrorCode}. 큐 보존.");
    }

    private async Task<long?> CreatePlaythroughAsync(string token)
    {
        ApiResult<PlaythroughCreatedDto> result =
            await _api.CreatePlaythroughAsync(_session.UserId.Value, token);

        if (!result.Ok)
        {
            if (!result.NetworkError)
                Debug.LogWarning($"[동기화] 회차 생성 실패 — HTTP {result.Status} {result.ErrorCode}");

            return null;
        }

        _queue.SetPlaythroughId(result.Body.PlaythroughId);

        Debug.Log($"[동기화] 회차 생성 — playthroughId {result.Body.PlaythroughId}");

        return result.Body.PlaythroughId;
    }
}