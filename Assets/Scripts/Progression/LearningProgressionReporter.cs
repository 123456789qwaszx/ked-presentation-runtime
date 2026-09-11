using System;
using UnityEngine;

// U0 관찰용 연결. 저장 성공 뒤에만 디스크에 확정된 snapshot을 읽는다.
// HTTP 요청, 별도 저장 파일, 기존 동기화 상태의 ACK를 만들지 않는다.
public sealed class LearningProgressionReporter : IProgressionReporter
{
    private readonly SaveCoordinator _save;
    private readonly ILocalSaveStore _localStore;
    private readonly Action<string> _log;

    public LearningProgressionReporter(
        SaveCoordinator save,
        ILocalSaveStore localStore,
        Action<string> log = null)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _localStore = localStore ?? throw new ArgumentNullException(nameof(localStore));
        _log = log ?? (message => Debug.Log(message));
    }

    public void ReportSceneEntered(SceneEntryReport report)
    {
        _save.ReportSceneEntered(report);
        LogSnapshot("SceneEntered", report.ChapterId, report.State.CurrentEpisodeId);
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
