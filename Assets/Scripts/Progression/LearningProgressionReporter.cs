using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 학습 연결:
// - 기존 SaveCoordinator에 먼저 위임해 로컬 저장 경계를 보존한다.
// - 저장 성공 뒤 확정 snapshot을 관찰한다.
// - U1에서는 첫 SceneEntered의 chapterKey로 학습 서버 콘텐츠를 한 번 조회한다.
public sealed class LearningProgressionReporter : IProgressionReporter
{
    private readonly SaveCoordinator _save;
    private readonly ILocalSaveStore _localStore;
    private readonly AnalyticsApi _analyticsApi;
    private readonly Action<string> _log;

    private bool _catalogLookupStarted;

    public LearningProgressionReporter(
        SaveCoordinator save,
        ILocalSaveStore localStore,
        Action<string> log = null,
        string analyticsBaseUrl = "http://localhost:8080")
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _localStore = localStore ?? throw new ArgumentNullException(nameof(localStore));
        _analyticsApi = new AnalyticsApi(analyticsBaseUrl);
        _log = log ?? (message => Debug.Log(message));
    }

    public void ReportSceneEntered(SceneEntryReport report)
    {
        _save.ReportSceneEntered(report);
        LogSnapshot("SceneEntered", report.ChapterId, report.State.CurrentEpisodeId);

        if (!_catalogLookupStarted)
        {
            _catalogLookupStarted = true;
            _ = LookupChapterAsync(report.ChapterId);
        }
    }

    public void ReportSceneCommitted(SceneCommitReport report)
    {
        _save.ReportSceneCommitted(report);
        LogSnapshot("SceneCommitted", report.ChapterId, report.State.CurrentEpisodeId);
    }

    private async Task LookupChapterAsync(string chapterKey)
    {
        LearningAnalyticsOverlay.Show(
            $"[U1 서버 콘텐츠]\nlocal chapterKey: {chapterKey}\n조회 중...");

        try
        {
            AnalyticsApiResult<List<AnalyticsChapterSummaryDto>> result =
                await _analyticsApi.FindChaptersAsync(chapterKey);

            if (result.NetworkError)
            {
                string message =
                    $"[U1 서버 콘텐츠]\nlocal chapterKey: {chapterKey}\n통신 실패: {result.ErrorMessage}";

                LearningAnalyticsOverlay.Show(message);
                Debug.LogWarning(message);
                return;
            }

            if (!result.IsSuccess)
            {
                string message =
                    $"[U1 서버 콘텐츠]\nlocal chapterKey: {chapterKey}\n" +
                    $"HTTP {result.Status} {result.ErrorCode}: {result.ErrorMessage}";

                LearningAnalyticsOverlay.Show(message);
                Debug.LogWarning(message);
                return;
            }

            if (result.Body.Count == 0)
            {
                string message =
                    $"[U1 서버 콘텐츠]\nlocal chapterKey: {chapterKey}\n서버에 등록되지 않음";

                LearningAnalyticsOverlay.Show(message);
                Debug.Log(message);
                return;
            }

            AnalyticsChapterSummaryDto chapter = result.Body[0];

            string success =
                $"[U1 서버 콘텐츠]\n" +
                $"local chapterKey: {chapterKey}\n" +
                $"server chapterId: {chapter.ChapterId} / title: {chapter.Title}";

            LearningAnalyticsOverlay.Show(success);
            Debug.Log(success);
        }
        catch (Exception error)
        {
            string message =
                $"[U1 서버 콘텐츠]\nlocal chapterKey: {chapterKey}\n조회 처리 실패: {error.Message}";

            LearningAnalyticsOverlay.Show(message);
            Debug.LogWarning(message);
        }
    }

    private void LogSnapshot(string boundary, string chapterId, string episodeId)
    {
        // 저장 호출 뒤에 ID를 확보한다. 첫 SceneEntered에서 회차가 만들어진다.
        string id = _save.PlaythroughId;

        try
        {
            LocalSaveFile snapshot = _localStore.LoadPlaythrough(id);

            if (snapshot == null)
                throw new InvalidOperationException("확정된 로컬 snapshot을 찾을 수 없다.");

            SceneRecord lastScene = snapshot.Scenes.Count == 0
                ? null
                : snapshot.Scenes[snapshot.Scenes.Count - 1];

            _log($"[학습] {boundary} — {chapterId}/{episodeId}\n" +
                 SaveJson.SerializePretty(new
                 {
                     ClientPlaythroughId = id,
                     snapshot.ChapterId,
                     snapshot.CurrentEpisodeId,
                     snapshot.ChapterCompleted,
                     SceneCount = snapshot.Scenes.Count,
                     LastCommittedPath = lastScene?.Path,
                     snapshot.Stats,
                     YarnVariableCount = snapshot.Variables?.Count ?? 0,
                     BacklogCount = snapshot.Backlog.Count,
                 }));
        }
        catch (Exception error)
        {
            // 관찰 실패가 이미 성공한 저장과 다음 장면 진행을 취소해서는 안 된다.
            Debug.LogWarning($"[학습] 저장은 성공했지만 관찰 로그를 만들지 못했다: {error.Message}");
        }
    }
}
