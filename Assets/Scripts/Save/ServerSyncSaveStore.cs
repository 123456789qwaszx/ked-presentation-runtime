using System;
using System.Threading.Tasks;
using UnityEngine;

// 큐 → 서버 (M7). 로컬이 진실이고 여기는 사본을 미는 곳 — 실패하면 큐가 남아 다음 기회에 다시 온다.
//
// 한 번의 동기화: 토큰 → 회차(없으면 POST) → 버전(D-015) → 배치 캡처 → PUT → 200이면 배치 삭제.
// 어느 단계든 안 되면 조용히 접는다. 오프라인이 정상 경로다.
public sealed class ServerSyncSaveStore
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly SyncQueue _queue;
    private readonly ChapterVersionResolver _versionResolver;
    private readonly ISaveStore _localStore;
    private readonly string _deviceKey;

    private bool _syncing;
    private bool _syncAgain;

    // 409 — 다른 기기가 먼저 저장했다. M7은 알리기만 하고 큐를 보존한다. 해소(폐기 vs force)는 M8.
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

    // 겹쳐 불러도 안전하다 — 도는 중이면 "끝나고 한 번 더"만 표시한다.
    // 이 메서드는 기다리는 이 없이(fire-and-forget) 불리므로 예외를 밖으로 내지 않는다.
    public async Task TrySyncAsync(int slotNo)
    {
        if (_syncing)
        {
            _syncAgain = true;
            return;
        }

        _syncing = true;

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
            _syncing = false;

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

        // 스냅샷이 없으면 보낼 것도 없다 — 큐는 스냅샷과 같은 커밋에서 쌓인다.
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

        // 토큰이 죽어 있었다(서버 재시작 등). 한 번만 새로 받아 다시 민다.
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
