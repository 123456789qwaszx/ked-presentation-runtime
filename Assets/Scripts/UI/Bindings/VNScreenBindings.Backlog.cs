public sealed partial class VNScreenBindings
{
    private ProgressionLauncher _progressionLauncher;
    private SaveCoordinator _saveCoordinator;

    // 갈라지기(이전 장면 루트로)에 필요한 둘. 진행 층 없이 도는 디버그 경로면 null.
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
        AddBinding(panel,
            p => p.OnCloseRequested += ClosePanel,
            p => p.OnCloseRequested -= ClosePanel);

        AddBinding(panel,
            p => p.OnJumpRequested += HandleBacklogJump,
            p => p.OnJumpRequested -= HandleBacklogJump);
    }

    private bool CanActOn(DialogueLogEntry entry) =>
        _vnFeatures.CanJumpTo(entry) ||
        (_saveCoordinator != null && _progressionLauncher != null && _saveCoordinator.CanForkTo(entry.lineSerial));

    private async void HandleBacklogJump(DialogueLogEntry entry)
    {
        // 현재 장면(아직 pending). 롤백과 동일
        if (_vnFeatures.RequestBacklogJump(entry))
        {
            ClosePanel();
            await _episodePlayer.RequestReplayAsync();
            return;
        }

        // 이전 장면(이미 Committed) 개념적으로 새 회차 시작 + 장면 루트에서 그 라인까지 재생.
        int sceneIndex;

        if (!_saveCoordinator.TryMakeLineTarget(entry, out sceneIndex, out SaveLineTarget target))
        {
            sceneIndex = _saveCoordinator.FindSceneIndexBySerial(entry.lineSerial);

            if (sceneIndex < 0)
                return;
        }

        ClosePanel();

        await _progressionLauncher.StopAsync();
        await _saveCoordinator.ForkFromScene(sceneIndex, target);
        await _progressionLauncher.LaunchAsync();
    }
}
