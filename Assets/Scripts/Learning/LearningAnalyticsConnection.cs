using System;
using System.Threading.Tasks;
using UnityEngine;

// 학습 HTTP와 표시를 저장 관찰자에서 분리한다.
// U2: 로컬 회차를 서버 회차에 연결한다.
// U3: 마지막으로 로컬 저장에 성공한 snapshot을 명시적으로 서버에 백업한다.
public sealed class LearningAnalyticsConnection
{
    private readonly string _baseUrl;
    private readonly AnalyticsApi _api;

    private string _chapterKey;
    private string _clientPlaythroughId;
    private Task _connectionTask;
    private Task _backupTask;

    public LearningAnalyticsConnection(string baseUrl)
    {
        _baseUrl = baseUrl;
        _api = new AnalyticsApi(baseUrl);

        LearningAnalyticsOverlay.SetBackupAction(BackupLatest);
        Publish($"[학습 서버] 연결 초기화\nserver: {_baseUrl}\n첫 장면 진입 대기");
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
