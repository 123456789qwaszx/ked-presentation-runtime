using Ked.Progression.Save;

// 정상적으로 표시가 끝난 줄을 현재 회차의 이어하기 지점으로 저장한다.
// 줄 표시 시점과 Progression/저장 상태를 잇는 게임 조립 계층이다.
public sealed class ActiveResumeSaveFlow
{
    private readonly SaveCoordinator _saveCoordinator;
    private readonly ProgressionLauncher _progressionLauncher;
    private readonly VNFeatureController _vnFeatures;

    public ActiveResumeSaveFlow(
        SaveCoordinator saveCoordinator,
        ProgressionLauncher progressionLauncher,
        VNFeatureController vnFeatures)
    {
        _saveCoordinator = saveCoordinator;
        _progressionLauncher = progressionLauncher;
        _vnFeatures = vnFeatures;
    }

    public void Update()
    {
        if (!_progressionLauncher.IsRunning)
            return;

        if (!_vnFeatures.TryGetCurrentLine(out LineTarget target, out _))
            return;

        _saveCoordinator.UpdateResumePoint(
            _progressionLauncher.PendingPath,
            PresentationMemory.ResumePoint(target, _vnFeatures.CreateYarnChoiceSnapshot()));
    }
}
