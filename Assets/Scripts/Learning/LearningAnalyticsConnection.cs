using System;
using System.Threading.Tasks;
using UnityEngine;

// 학습 HTTP와 표시를 저장 관찰자에서 분리한다.
// U2: 로컬 회차를 서버 회차에 연결한다.
// U3: 마지막으로 로컬 저장에 성공한 snapshot을 명시적으로 서버에 백업한다.
// U4: 서버 checkpoint를 LocalSaveFile로 복원하고 기존 Progression 재개 경계로 실행한다.
public sealed class LearningAnalyticsConnection
{
    private readonly string _baseUrl;
    private readonly AnalyticsApi _api;

    private ProgressionLauncher _progressionLauncher;
    private string _chapterKey;
    private string _clientPlaythroughId;
    private Task _connectionTask;
    private Task _backupTask;
    private Task _restoreCheckTask;
    private Task _restoreTask;

    public LearningAnalyticsConnection(string baseUrl)
    {
        _baseUrl = baseUrl;
        _api = new AnalyticsApi(baseUrl);

        LearningAnalyticsOverlay.SetBackupAction(BackupLatest);
        LearningAnalyticsOverlay.SetRestoreCheckAction(CheckServerSnapshot);
        LearningAnalyticsOverlay.SetRestoreAction(RestoreFromServer);
        Publish($"[학습 서버] 연결 초기화\nserver: {_baseUrl}\n첫 장면 진입 대기");
    }

    public void BindProgressionLauncher(ProgressionLauncher progressionLauncher)
    {
        _progressionLauncher = progressionLauncher;
    }

    public void OnSceneEntered(
        string chapterKey,
        string clientPlaythroughId)
    {
        bool samePlaythrough =
            _chapterKey == chapterKey
            && _clientPlaythroughId == clientPlaythroughId;

        if (samePlaythrough)
            return;

        _chapterKey = chapterKey;
        _clientPlaythroughId = clientPlaythroughId;
        Retry();
    }

    public void Retry()
    {
        if (string.IsNullOrEmpty(_chapterKey)
            || string.IsNullOrEmpty(_clientPlaythroughId))
        {
            Publish("[학습 서버] 새 게임 또는 이어하기로 첫 장면에 진입하세요.");
            return;
        }

        if (_connectionTask != null && !_connectionTask.IsCompleted)
            return;

        string chapterKey = _chapterKey;
        string clientPlaythroughId = _clientPlaythroughId;

        _connectionTask = ConnectAsync(chapterKey, clientPlaythroughId);
    }

    public void BackupLatest()
    {
        if (_backupTask != null && !_backupTask.IsCompleted)
            return;

        LocalSaveFile snapshot = LearningSession.LatestSnapshot;
        long? serverPlaythroughId = LearningSession.ServerPlaythroughId;

        if (snapshot == null)
        {
            Publish("[U3 서버 백업] 아직 로컬에 확정된 snapshot이 없습니다.", warning: true);
            return;
        }

        if (!serverPlaythroughId.HasValue)
        {
            Publish("[U3 서버 백업] 서버 회차 연결이 먼저 필요합니다.", warning: true);
            return;
        }

        string clientPlaythroughId = snapshot.PlaythroughId;
        long capturedServerId = serverPlaythroughId.Value;
        string snapshotJson = SaveJson.Serialize(snapshot);

        _backupTask = BackupAsync(
            capturedServerId,
            clientPlaythroughId,
            snapshot.CurrentEpisodeId,
            snapshot.ChapterCompleted,
            snapshotJson);
    }

    public void CheckServerSnapshot()
    {
        if (_restoreCheckTask != null && !_restoreCheckTask.IsCompleted)
            return;

        long? serverPlaythroughId = LearningSession.ServerPlaythroughId;
        string clientPlaythroughId = LearningSession.ClientPlaythroughId;
        string chapterKey = LearningSession.ChapterKey;

        if (!serverPlaythroughId.HasValue
            || string.IsNullOrEmpty(clientPlaythroughId)
            || string.IsNullOrEmpty(chapterKey))
        {
            Publish("[U4 서버 복원 확인] 서버 회차 연결이 먼저 필요합니다.", warning: true);
            return;
        }

        _restoreCheckTask = CheckServerSnapshotAsync(
            serverPlaythroughId.Value,
            clientPlaythroughId,
            chapterKey);
    }

    public void RestoreFromServer(long serverPlaythroughId)
    {
        if (_restoreTask != null && !_restoreTask.IsCompleted)
            return;

        ILocalSaveStore localStore = LearningSession.LocalStore;

        if (localStore == null)
        {
            Publish("[U4 서버 복원] 로컬 저장소가 아직 준비되지 않았습니다.", warning: true);
            return;
        }

        if (_progressionLauncher == null)
        {
            Publish("[U4 서버 복원] 진행 재개 경계가 아직 준비되지 않았습니다.", warning: true);
            return;
        }

        _restoreTask = RestoreFromServerAsync(serverPlaythroughId, localStore);
    }

    private async Task ConnectAsync(
        string chapterKey,
        string clientPlaythroughId)
    {
        Publish(
            $"[U2 서버 회차] 연결 시작\n" +
            $"chapterKey: {chapterKey}\n" +
            $"clientPlaythroughId: {clientPlaythroughId}");

        try
        {
            AnalyticsApiResult<AnalyticsPlaythroughDto> result =
                await _api.CreateOrGetPlaythroughAsync(
                    chapterKey,
                    clientPlaythroughId);

            if (_chapterKey != chapterKey
                || _clientPlaythroughId != clientPlaythroughId)
            {
                Debug.Log(
                    $"[U2 서버 회차] 이전 회차 응답 무시\n" +
                    $"clientPlaythroughId: {clientPlaythroughId}");
                return;
            }

            string prefix =
                $"[U2 서버 회차]\n" +
                $"chapterKey: {chapterKey}\n" +
                $"clientPlaythroughId: {clientPlaythroughId}\n";

            if (result.NetworkError)
            {
                Publish(prefix + $"통신 실패: {result.ErrorMessage}", warning: true);
                return;
            }

            if (!result.IsSuccess)
            {
                Publish(
                    prefix +
                    $"HTTP {result.Status} {result.ErrorCode}: {result.ErrorMessage}",
                    warning: true);
                return;
            }

            LearningSession.BindServer(
                clientPlaythroughId,
                result.Body.PlaythroughId);

            Publish(
                prefix +
                $"HTTP {result.Status}\n" +
                $"server playthroughId: {result.Body.PlaythroughId}");
        }
        catch (Exception error)
        {
            Publish($"[U2 서버 회차] 연결 처리 실패\n{error}", warning: true);
        }
    }

    private async Task BackupAsync(
        long serverPlaythroughId,
        string clientPlaythroughId,
        string episodeKey,
        bool chapterCompleted,
        string snapshotJson)
    {
        Publish(
            $"[U3 서버 백업] 전송 시작\n" +
            $"server playthroughId: {serverPlaythroughId}\n" +
            $"episodeKey: {episodeKey} / completed: {chapterCompleted}");

        try
        {
            AnalyticsApiResult<AnalyticsCheckpointDto> result =
                await _api.SaveCheckpointAsync(
                    serverPlaythroughId,
                    episodeKey,
                    chapterCompleted,
                    snapshotJson);

            if (LearningSession.ClientPlaythroughId != clientPlaythroughId
                || LearningSession.ServerPlaythroughId != serverPlaythroughId)
            {
                Debug.Log(
                    $"[U3 서버 백업] 이전 회차 응답 무시\n" +
                    $"clientPlaythroughId: {clientPlaythroughId}");
                return;
            }

            string prefix =
                $"[U3 서버 백업]\n" +
                $"server playthroughId: {serverPlaythroughId}\n";

            if (result.NetworkError)
            {
                Publish(prefix + $"통신 실패: {result.ErrorMessage}", warning: true);
                return;
            }

            if (!result.IsSuccess)
            {
                Publish(
                    prefix +
                    $"HTTP {result.Status} {result.ErrorCode}: {result.ErrorMessage}",
                    warning: true);
                return;
            }

            Publish(
                prefix +
                $"HTTP {result.Status}\n" +
                $"checkpointId: {result.Body.CheckpointId}\n" +
                $"saved episodeKey: {result.Body.EpisodeKey}");
        }
        catch (Exception error)
        {
            Publish($"[U3 서버 백업] 처리 실패\n{error}", warning: true);
        }
    }

    private async Task CheckServerSnapshotAsync(
        long serverPlaythroughId,
        string clientPlaythroughId,
        string chapterKey)
    {
        Publish(
            $"[U4 서버 복원 확인] 조회 시작\n" +
            $"server playthroughId: {serverPlaythroughId}");

        try
        {
            AnalyticsApiResult<AnalyticsCheckpointDto> result =
                await _api.GetCheckpointAsync(serverPlaythroughId);

            if (LearningSession.ClientPlaythroughId != clientPlaythroughId
                || LearningSession.ServerPlaythroughId != serverPlaythroughId)
            {
                Debug.Log(
                    $"[U4 서버 복원 확인] 이전 회차 응답 무시\n" +
                    $"clientPlaythroughId: {clientPlaythroughId}");
                return;
            }

            string prefix =
                $"[U4 서버 복원 확인]\n" +
                $"server playthroughId: {serverPlaythroughId}\n";

            if (result.NetworkError)
            {
                Publish(prefix + $"통신 실패: {result.ErrorMessage}", warning: true);
                return;
            }

            if (!result.IsSuccess)
            {
                Publish(
                    prefix +
                    $"HTTP {result.Status} {result.ErrorCode}: {result.ErrorMessage}",
                    warning: true);
                return;
            }

            if (result.Status == 204 || result.Body == null)
            {
                Publish(prefix + "HTTP 204\n서버 checkpoint 없음");
                return;
            }

            AnalyticsCheckpointDto checkpoint = result.Body;
            LocalSaveFile snapshot = ReadAndValidateSnapshot(
                checkpoint,
                expectedClientPlaythroughId: clientPlaythroughId,
                expectedChapterKey: chapterKey);

            Publish(
                prefix +
                $"HTTP {result.Status}\n" +
                $"deserialize 성공\n" +
                $"episodeKey: {snapshot.CurrentEpisodeId}\n" +
                $"completed: {snapshot.ChapterCompleted}\n" +
                $"sceneCount: {snapshot.Scenes?.Count ?? 0}");
        }
        catch (Exception error)
        {
            Publish($"[U4 서버 복원 확인] 처리 실패\n{error.Message}", warning: true);
        }
    }

    private async Task RestoreFromServerAsync(
        long serverPlaythroughId,
        ILocalSaveStore localStore)
    {
        Publish(
            $"[U4 서버 복원] 조회 시작\n" +
            $"server playthroughId: {serverPlaythroughId}");

        try
        {
            AnalyticsApiResult<AnalyticsCheckpointDto> result =
                await _api.GetCheckpointAsync(serverPlaythroughId);

            string prefix =
                $"[U4 서버 복원]\n" +
                $"server playthroughId: {serverPlaythroughId}\n";

            if (result.NetworkError)
            {
                Publish(prefix + $"통신 실패: {result.ErrorMessage}", warning: true);
                return;
            }

            if (!result.IsSuccess)
            {
                Publish(
                    prefix +
                    $"HTTP {result.Status} {result.ErrorCode}: {result.ErrorMessage}",
                    warning: true);
                return;
            }

            if (result.Status == 204 || result.Body == null)
            {
                Publish(prefix + "HTTP 204\n복원할 서버 checkpoint가 없습니다.", warning: true);
                return;
            }

            AnalyticsCheckpointDto checkpoint = result.Body;

            if (checkpoint.PlaythroughId != serverPlaythroughId)
                throw new InvalidOperationException("checkpoint의 server playthroughId가 요청과 다르다.");

            LocalSaveFile snapshot = ReadAndValidateSnapshot(checkpoint);

            if (localStore.LoadPlaythrough(snapshot.PlaythroughId) != null)
            {
                Publish(
                    prefix +
                    $"HTTP {result.Status}\n" +
                    $"복원 중단: 같은 로컬 회차가 이미 존재합니다.\n" +
                    $"clientPlaythroughId: {snapshot.PlaythroughId}",
                    warning: true);
                return;
            }

            Publish(
                prefix +
                $"HTTP {result.Status}\n" +
                $"snapshot 검증 완료\n" +
                $"episodeKey: {snapshot.CurrentEpisodeId}\n" +
                $"현재 재생 종료 후 로컬 복원 시작");

            await _progressionLauncher.TransitionAsync(() =>
            {
                if (localStore.LoadPlaythrough(snapshot.PlaythroughId) != null)
                {
                    throw new InvalidOperationException(
                        "재생 전환 중 같은 로컬 회차가 생겨 복원을 중단했다.");
                }

                localStore.Create(snapshot);
                localStore.SetActive(snapshot.PlaythroughId);

                LearningSession.Capture(snapshot);
                LearningSession.BindServer(
                    snapshot.PlaythroughId,
                    serverPlaythroughId);

                _chapterKey = snapshot.ChapterId;
                _clientPlaythroughId = snapshot.PlaythroughId;

                Publish(
                    prefix +
                    $"로컬 회차 생성 완료\n" +
                    $"clientPlaythroughId: {snapshot.PlaythroughId}\n" +
                    $"episodeKey: {snapshot.CurrentEpisodeId}\n" +
                    $"active 지정 완료\n" +
                    $"기존 이어하기 경계로 재생 시작");

                return Task.CompletedTask;
            });
        }
        catch (Exception error)
        {
            Publish($"[U4 서버 복원] 처리 실패\n{error.Message}", warning: true);
        }
    }

    private static LocalSaveFile ReadAndValidateSnapshot(
        AnalyticsCheckpointDto checkpoint,
        string expectedClientPlaythroughId = null,
        string expectedChapterKey = null)
    {
        LocalSaveFile snapshot =
            SaveJson.Deserialize<LocalSaveFile>(checkpoint.SnapshotJson);

        if (snapshot == null)
            throw new InvalidOperationException("snapshotJson을 LocalSaveFile로 읽지 못했다.");

        if (string.IsNullOrWhiteSpace(snapshot.PlaythroughId))
            throw new InvalidOperationException("snapshot의 clientPlaythroughId가 비어 있다.");

        if (string.IsNullOrWhiteSpace(snapshot.ChapterId))
            throw new InvalidOperationException("snapshot의 chapterId가 비어 있다.");

        if (snapshot.CurrentEpisodeId != checkpoint.EpisodeKey)
            throw new InvalidOperationException("snapshot의 currentEpisodeId가 checkpoint 메타데이터와 다르다.");

        if (snapshot.ChapterCompleted != checkpoint.ChapterCompleted)
            throw new InvalidOperationException("snapshot의 chapterCompleted가 checkpoint 메타데이터와 다르다.");

        if (expectedClientPlaythroughId != null
            && snapshot.PlaythroughId != expectedClientPlaythroughId)
        {
            throw new InvalidOperationException("snapshot의 clientPlaythroughId가 현재 회차와 다르다.");
        }

        if (expectedChapterKey != null
            && snapshot.ChapterId != expectedChapterKey)
        {
            throw new InvalidOperationException("snapshot의 chapterId가 현재 챕터와 다르다.");
        }

        return snapshot;
    }

    private static void Publish(string message, bool warning = false)
    {
        if (warning)
            Debug.LogWarning(message);
        else
            Debug.Log(message);

        try
        {
            LearningAnalyticsOverlay.Show(message);
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[학습 서버] 화면 표시 실패 (HTTP 연결은 계속 진행): {error}");
        }
    }
}
