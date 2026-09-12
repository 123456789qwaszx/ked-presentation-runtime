using System;
using System.Threading.Tasks;
using UnityEngine;

// 학습 HTTP와 표시를 저장 관찰자에서 분리한다. 표시 실패도 로컬 진행을 막지 않는다.
public sealed class LearningAnalyticsConnection
{
    private readonly string _baseUrl;
    private string _chapterKey;
    private Task _lookup;

    public LearningAnalyticsConnection(string baseUrl)
    {
        _baseUrl = baseUrl;
        Publish($"[U1 서버 콘텐츠] 연결 초기화\nserver: {_baseUrl}\n첫 장면 진입 대기");
    }

    public void OnSceneEntered(string chapterKey)
    {
        if (_chapterKey == chapterKey)
            return;

        _chapterKey = chapterKey;
        Retry();
    }

    public void Retry()
    {
        if (string.IsNullOrEmpty(_chapterKey))
        {
            Publish("[U1 서버 콘텐츠] 새 게임 또는 이어하기로 첫 장면에 진입하세요.");
            return;
        }

        if (_lookup != null && !_lookup.IsCompleted)
            return;

        _lookup = LookupChapterAsync(_chapterKey);
    }

    private async Task LookupChapterAsync(string chapterKey)
    {
        // HTTP 시작 전에 Console에도 기록한다. 화면 생성 성공 여부와 독립적이다.
        Publish($"[U1 서버 콘텐츠] 조회 시작\nGET {_baseUrl}/chapters?chapterKey={Uri.EscapeDataString(chapterKey)}");

        try
        {
            var api = new AnalyticsApi(_baseUrl);
            var result = await api.FindChaptersAsync(chapterKey);
            string prefix = $"[U1 서버 콘텐츠]\nlocal chapterKey: {chapterKey}\n";

            if (result.NetworkError)
            {
                Publish(prefix + $"통신 실패: {result.ErrorMessage}", warning: true);
                return;
            }

            if (!result.IsSuccess)
            {
                Publish(prefix + $"HTTP {result.Status} {result.ErrorCode}: {result.ErrorMessage}", warning: true);
                return;
            }

            if (result.Body.Count == 0)
            {
                Publish(prefix + $"HTTP {result.Status} / 서버에 등록되지 않음");
                return;
            }

            var chapter = result.Body[0];
            Publish(prefix + $"HTTP {result.Status}\nserver chapterId: {chapter.ChapterId} / title: {chapter.Title}");
        }
        catch (Exception error)
        {
            Publish($"[U1 서버 콘텐츠] 조회 처리 실패\n{error}", warning: true);
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
            Debug.LogWarning($"[U1 서버 콘텐츠] 화면 표시 실패 (HTTP 조회는 계속 진행): {error}");
        }
    }
}
