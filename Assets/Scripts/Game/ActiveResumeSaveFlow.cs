using Ked.Progression.Save;

// 정상적으로 표시가 끝난 줄을 현재 회차의 이어하기 지점으로 저장한다.
// 줄 표시 시점과 Progression/저장 상태를 잇는 게임 조립 계층이다.
// 재개 지점의 생김새는 모른다 — PlaybackMemory가 꺼내 준 문자열을 진행 저장에 넘길 뿐이다.
public sealed class ActiveResumeSaveFlow
{
    private readonly SaveCoordinator _saveCoordinator;
    private readonly ProgressionLauncher _progressionLauncher;
    private readonly PlaybackMemory _memory;

    public ActiveResumeSaveFlow(
        SaveCoordinator saveCoordinator,
        ProgressionLauncher progressionLauncher,
        PlaybackMemory memory)
    {
        _saveCoordinator = saveCoordinator;
        _progressionLauncher = progressionLauncher;
        _memory = memory;
    }

    public void Update()
    {
        if (!_progressionLauncher.IsRunning)
            return;

        if (!_memory.TryCaptureResumePoint(out string resumePoint, out _))
            return;

        _saveCoordinator.UpdateResumePoint(
            _progressionLauncher.PendingPath,
            resumePoint);
    }
}
