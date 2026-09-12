using System;
using System.Threading.Tasks;
using UnityEngine;

// 학습 HTTP와 표시를 저장 관찰자에서 분리한다.
// 로컬 저장 성공 뒤 chapterKey + clientPlaythroughId를 받아 서버 연결을 수행한다.
public sealed class LearningAnalyticsConnection
{
    private readonly string _baseUrl;
    private readonly AnalyticsApi _api;

    private string _chapterKey;
    private string _clientPlaythroughId;
    private Task _connectionTask;

    public LearningAnalyticsConnection(string baseUrl)
    {
        _baseUrl = baseUrl;
        _api = new AnalyticsApi(baseUrl);

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

            // 요청 중 새 게임으로 전환됐다면 예전 응답을 현재 회차 결과로 채택하지 않는다.
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
