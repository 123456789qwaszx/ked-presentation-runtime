using System.Collections.Generic;
using Ked.Progression;

// Progression의 Scene 경계와 Host의 실행 상태 사이를 잇는다.
//
// SceneRunner는 순수 Progression 데이터와 저장 시점만 넘긴다.
// Backlog와 Yarn 선택 기록은 이 자리에서 같은 시점의 snapshot으로 캡처하고,
// SaveCoordinator는 캡처된 값만 받아 저장 정책을 수행한다.
public sealed class ProgressionSaveBridge : IScenePersistence
{
    private readonly SaveCoordinator _saveCoordinator;
    private readonly BacklogRecorder _backlog;
    private readonly ChoiceHistory _choiceHistory;

    public ProgressionSaveBridge(
        SaveCoordinator saveCoordinator,
        BacklogRecorder backlog,
        ChoiceHistory choiceHistory)
    {
        _saveCoordinator = saveCoordinator;
        _backlog = backlog;
        _choiceHistory = choiceHistory;
    }

    public void EnterScene(
        string chapterId,
        ChapterState entryState)
    {
        _saveCoordinator.EnterScene(
            chapterId,
            entryState,
            _backlog.NextLineSequence);
    }

    public void CommitScene(
        string chapterId,
        SceneCommitResult result,
        SceneRunOutcome outcome)
    {
        List<VNChoiceRecord> yarnChoices =
            _choiceHistory.CreateChoiceSnapshot();

        var backlog =
            new List<DialogueLogEntry>(_backlog.Entries);

        _saveCoordinator.CommitScene(
            chapterId,
            result,
            yarnChoices,
            backlog,
            _backlog.NextLineSequence,
            outcome);
    }
}
