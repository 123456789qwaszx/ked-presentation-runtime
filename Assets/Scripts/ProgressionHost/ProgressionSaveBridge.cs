using System.Collections.Generic;
using Ked.Progression;

// 장면 경계에서 저장을 실행하는 자리.
//
// 진행 런타임은 순수 진행 데이터(SceneCommitResult, SceneRunOutcome)까지만 건네고,
// Yarn 선택 기록과 백로그는 여기서 그 순간에 캡처해 장면 기록으로 조립한다.
//
// ⚠ 스냅샷을 찍는 시점이 계약이다.
//   EnterScene   : BeginSceneAsync + MarkSceneStart 직후 — 변수 체크포인트와 백로그 순번이 장면 진입 값이다.
//   CommitScene  : 정상 장면 완료 지점 — Rollback과 Stop은 여기 오지 않는다.
//
// ⚠ 실패를 삼키지 않는다.
//   저장이 실패하면 현재 장면 실행도 실패하고 다음 장면으로 넘어가지 않는다.
//   디스크에는 이전 snapshot이 남는다. 그것이 IScenePersistence의 계약이다.
public sealed class ProgressionSaveBridge : IScenePersistence
{
    private readonly ISceneRecordReporter _records;
    private readonly BacklogRecorder _backlog;
    private readonly ChoiceHistory _choiceHistory;

    public ProgressionSaveBridge(
        ISceneRecordReporter records,
        BacklogRecorder backlog,
        ChoiceHistory choiceHistory)
    {
        _records = records;
        _backlog = backlog;
        _choiceHistory = choiceHistory;
    }

    public void EnterScene(
        string chapterId,
        string sceneId,
        ChapterState entryState)
    {
        _records.ReportSceneEntered(
            new SceneEntryReport(
                chapterId,
                entryState,
                _backlog.NextLineSequence));
    }

    public void CommitScene(
        string chapterId,
        string sceneId,
        SceneCommitResult result,
        SceneRunOutcome outcome)
    {
        _records.ReportSceneCommitted(
            new SceneCommitReport(
                chapterId,
                result.Choices,
                _choiceHistory.CreateChoiceSnapshot(),
                result.WatchedEpisodeIds,
                result.State,
                new List<DialogueLogEntry>(_backlog.Entries),
                _backlog.NextLineSequence,
                outcome == SceneRunOutcome.ChapterEnded));
    }
}
