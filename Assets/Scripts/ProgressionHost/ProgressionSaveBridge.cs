using Ked.Progression;
using Ked.Progression.Save;

// Progression의 Scene 경계와 Host의 실행 상태 사이를 잇는다.
//
// SceneRunner는 순수 Progression 데이터와 저장 시점만 넘긴다.
// 백로그와 Yarn 선택 기록은 이 자리에서 같은 시점의 문자열로 싣고,
// SaveCoordinator는 그 문자열이 무엇인지 모른 채 저장 정책만 수행한다.
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
            hostSerialStart: _backlog.NextLineSequence);
    }

    public void CommitScene(
        string chapterId,
        SceneCommitResult result,
        SceneRunOutcome outcome)
    {
        _saveCoordinator.CommitScene(
            chapterId,
            result,
            sceneHostState: PresentationMemory.UnitChoices(_choiceHistory.CreateChoiceSnapshot()),
            playthroughHostState: PresentationMemory.Backlog(_backlog.Entries),
            hostSerialEnd: _backlog.NextLineSequence,
            outcome);
    }
}
