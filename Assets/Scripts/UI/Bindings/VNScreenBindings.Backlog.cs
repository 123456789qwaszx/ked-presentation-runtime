public sealed partial class VNScreenBindings
{
    private ProgressionLauncher _progressionLauncher;
    private SaveCoordinator _saveCoordinator;

    // 백로그 갈라지기는 진행 계층과 저장 계층이 모두 필요.
    // 진행 계층 없이 도는 디버그 경로에서는 null일 수 있음.
    public void ConfigureProgression(
        ProgressionLauncher launcher,
        SaveCoordinator saveCoordinator)
    {
        _progressionLauncher = launcher;
        _saveCoordinator = saveCoordinator;
    }

    private void OpenBacklogPanel()
    {
        UI.PushPanel<BacklogPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            panel.Present(_vnFeatures.Backlogs, CanActOn);
        });
    }

    private void ApplyBindings(BacklogPanel panel)
    {
        AddBinding(
            panel,
            p => p.OnCloseRequested += ClosePanel,
            p => p.OnCloseRequested -= ClosePanel);

        AddBinding(
            panel,
            p => p.OnJumpRequested += HandleBacklogJump,
            p => p.OnJumpRequested -= HandleBacklogJump);
    }

    private bool CanActOn(DialogueLogEntry entry)
    {
        // 실제 점프/replay를 수행하려면 launcher가 필요.
        if (_progressionLauncher == null)
            return false;

        // 현재 Scene 내부 롤백.
        if (_vnFeatures.CanJumpTo(entry))
            return true;

        // 완료된 이전 Scene이라면 새 회차로 갈라질 수 있다.
        return _saveCoordinator != null &&
               _saveCoordinator.CanForkFrom(entry);
    }

    private async void HandleBacklogJump(DialogueLogEntry entry)
    {
        if (_progressionLauncher == null)
            return;

        // 현재 Scene:
        // 아직 commit되지 않았으므로 기존 rollback/replay 경로를 사용.
        if (_vnFeatures.RequestBacklogJump(entry))
        {
            ClosePanel();

            await _progressionLauncher.RequestReplayAsync();
            return;
        }

        // 이전 Scene:
        // SaveCoordinator가
        //  - 어느 Scene인지
        //  - 정확한 라인까지 replay 가능한지
        //  - 불가능하면 Scene 루트로 fallback할지
        // 를 모두 판단.
        if (_saveCoordinator == null ||
            !_saveCoordinator.TryResolveForkTarget(entry, out SaveForkTarget forkTarget))
            return;

        ClosePanel();

        await _progressionLauncher.StopAsync();
        await _saveCoordinator.ForkFromScene(forkTarget);
        await _progressionLauncher.LaunchAsync();
    }
}