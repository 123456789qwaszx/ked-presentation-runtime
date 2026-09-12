using System;
using UnityEngine;

// 학습 연결:
// - 기존 SaveCoordinator에 먼저 위임해 로컬 저장 경계를 보존한다.
// - 저장 성공 뒤 확정 snapshot을 관찰한다.
// - U1에서는 첫 SceneEntered의 chapterKey로 학습 서버 콘텐츠를 한 번 조회한다.
public sealed class LearningProgressionReporter : IProgressionReporter
{
    private readonly SaveCoordinator _save;
    private readonly ILocalSaveStore _localStore;
    private readonly Action<string> _onSceneEntered;
    private readonly Action<string> _log;

    public LearningProgressionReporter(
        SaveCoordinator save,
        ILocalSaveStore localStore,
        Action<string> log = null,
        Action<string> onSceneEntered = null)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _localStore = localStore ?? throw new ArgumentNullException(nameof(localStore));
        _onSceneEntered = onSceneEntered;
        _log = log ?? (message => Debug.Log(message));
    }

    public void ReportSceneEntered(SceneEntryReport report)
    {
        _save.ReportSceneEntered(report);
        LogSnapshot("SceneEntered", report.ChapterId, report.State.CurrentEpisodeId);

        try
        {
            _onSceneEntered?.Invoke(report.ChapterId);
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[U1 서버 콘텐츠] 연결 시작 실패: {error}");
        }
    }

    public void ReportSceneCommitted(SceneCommitReport report)
    {
        _save.ReportSceneCommitted(report);
        LogSnapshot("SceneCommitted", report.ChapterId, report.State.CurrentEpisodeId);
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
