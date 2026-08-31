using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Ked.Save
{
    // 큐 → 서버 (M7-6). 로컬이 진실이고 여기는 사본을 미는 곳 —
    // 어떤 실패도 게임 진행을 막지 않고, 실패하면 큐가 남아 다음 기회에 다시 온다.
    //
    // 한 번의 동기화가 하는 일, 순서대로:
    //   토큰 확보(게스트 가입·로그인 포함) → 회차 확보(없으면 POST) → 버전 해석(D-015)
    //   → 큐 배치 캡처 → PUT → 200이면 배치 삭제 + revision 보관.
    // 어느 단계든 안 되면 조용히 접는다 — 오프라인이 정상 경로다.
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

        // 409 CONFLICT — 다른 기기가 먼저 저장했다. M7은 알리기만 하고 큐를 보존한다.
        // 해소(폐기 vs force)는 M8의 일이다. 인자는 서버가 준 응답 본문(현재 서버 상태 포함).
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

        // 몇 번을 겹쳐 불러도 안전하다: 도는 중이면 "끝나고 한 번 더"만 표시한다.
        // 커밋마다 부르는 쪽(SaveCoordinator)이 이 성질에 기댄다.
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
                // 동기화의 버그가 게임을 죽이면 안 된다 — 남기고 접는다.
                Debug.LogError($"[동기화] 예기치 못한 실패\n{error}");
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

            // 보낼 스냅샷이 없으면 보낼 것도 없다 — 큐는 스냅샷과 같은 커밋에서 쌓이므로
            // 스냅샷 없이 큐만 있는 상태는 정상적으론 없다.
            if (save == null)
                return;

            string token = await _session.EnsureTokenAsync();

            if (token == null)
                return;

            long? playthroughId = _queue.PlaythroughId;

            if (playthroughId == null)
            {
                playthroughId = await CreatePlaythroughAsync(token);

                if (playthroughId == null)
                    return;
            }

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
                BaseRevision = _queue.BaseRevision ?? 0,   // 신규 슬롯은 0 — 서버 규칙
                Choices = batch.Choices,
                Events = batch.Events,
            };

            ApiResult<SaveUploadResponseDto> result =
                await _api.PutSaveAsync(playthroughId.Value, slotNo, request, token);

            // 토큰이 죽어 있었다(서버 재시작 등). 한 번만 새로 받아 다시 민다.
            if (!result.Ok && result.Status == 401)
            {
                _session.InvalidateToken();
                token = await _session.EnsureTokenAsync();

                if (token == null)
                    return;

                result = await _api.PutSaveAsync(playthroughId.Value, slotNo, request, token);
            }

            if (result.Ok)
            {
                // 판정은 이것뿐이다: 200이면 큐를 비운다. acceptedChoices/Events를 보낸 수와
                // 비교하지 않는다 — 흡수(D-011)와 재전송(replayed)에서 작게 오는 것이 정상이다.
                _queue.Acknowledge(batch, result.Body.Revision);

                Debug.Log($"[동기화] 완료 — revision {result.Body.Revision}, "
                          + $"선택 {batch.Choices.Count}건, 이벤트 {batch.Events.Count}건 전송"
                          + (result.Body.Replayed ? " (재전송 흡수)" : ""));

                return;
            }

            if (result.ErrorCode == "CONFLICT")
            {
                // 큐도 baseRevision도 그대로 둔다 — 해소 전까지 같은 409가 반복되는 것이
                // 맞는 상태다. 덮어쓸지(force) 버릴지는 사용자의 결정이고, 그 UI는 M8이다.
                Debug.LogWarning("[동기화] 충돌(409) — 다른 기기가 먼저 저장했다. M8에서 해소.");
                ConflictDetected?.Invoke(result.RawBody);

                return;
            }

            if (result.NetworkError)
                return;   // 오프라인 — 정상 경로. 큐가 남는다.

            Debug.LogWarning($"[동기화] 실패 — HTTP {result.Status} {result.ErrorCode}. 큐 보존.");
        }

        private async Task<long?> CreatePlaythroughAsync(string token)
        {
            long? userId = _session.UserId;

            if (userId == null)
                return null;

            ApiResult<PlaythroughCreatedDto> result =
                await _api.CreatePlaythroughAsync(userId.Value, token);

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
}
