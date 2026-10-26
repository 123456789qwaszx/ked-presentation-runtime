using Ked.Progression;
using Ked.Progression.Save;

// Progression의 Scene 경계와 Host의 실행 상태 사이를 잇는다.
//
// SceneRunner는 순수 Progression 데이터와 저장 시점만 넘긴다.
// 연출 기억(백로그, 이 장면의 Yarn 선택 기록)은 이 자리에서 같은 시점의 문자열로 꺼내 싣고,
// SaveCoordinator는 그 문자열이 무엇인지 모른 채 저장 정책만 수행한다.
// 이 브리지도 문자열의 생김새를 모른다 — PlaybackMemory가 꺼내 준 것을 나를 뿐이다.
public sealed class ProgressionSaveBridge : IScenePersistence
{
    private readonly SaveCoordinator _saveCoordinator;
    private readonly PlaybackMemory _memory;

    public ProgressionSaveBridge(
        SaveCoordinator saveCoordinator,
        PlaybackMemory memory)
    {
        _saveCoordinator = saveCoordinator;
        _memory = memory;
    }

    public void EnterScene(
        string chapterId,
        ChapterState entryState)
    {
        _saveCoordinator.EnterScene(
            chapterId,
            entryState,
            hostSerialStart: _memory.NextSerial);
    }

    public void CommitScene(
        string chapterId,
        SceneCommitResult result,
        SceneRunOutcome outcome)
    {
        _saveCoordinator.CommitScene(
            chapterId,
            result,
            sceneHostState: _memory.CaptureUnitChoices(),
            playthroughHostState: _memory.CaptureBacklog(),
            hostSerialEnd: _memory.NextSerial,
            outcome);
    }
}
