public sealed partial class VNScreenBindings
{
    private void OpenBacklogPanel()
    {
        UI.PushPanel<BacklogPanel>(panel =>
        {
            BindView(panel, ApplyBindings);
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
        return _manualSaveFlow != null &&
               _manualSaveFlow.CanForkFrom(entry);
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
        // ManualSaveFlow가
        //  - 어느 Scene인지
        //  - 정확한 라인까지 replay 가능한지
        //  - 불가능하면 Scene 루트로 fallback할지
        // 를 모두 판단하고 회차 전환까지 맡는다.
        if (_manualSaveFlow == null)
            return;

        await _manualSaveFlow.ForkFromBacklogAsync(entry, ClosePanel);
    }
}
