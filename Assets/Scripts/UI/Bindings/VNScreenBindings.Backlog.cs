public sealed partial class VNScreenBindings
{
    private ProgressionLauncher _progressionLauncher;
    private SaveCoordinator _saveCoordinator;

    // 갈라지기(이전 장면 루트로)에 필요한 둘. 진행 층 없이 도는 디버그 경로면 null.
    public void ConfigureProgression(ProgressionLauncher launcher, SaveCoordinator saveCoordinator)
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

    // 현재 장면 항목은 되감기(롤백), 이전 장면 항목은 갈라지기. 둘 다 아니면(지금 라인·다른 챕터) 흐림.
    private bool CanActOn(DialogueLogEntry entry) =>
        _vnFeatures.CanJumpTo(entry) ||
        (_saveCoordinator != null && _progressionLauncher != null && _saveCoordinator.CanForkTo(entry.lineSerial));

    private async void HandleBacklogJump(DialogueLogEntry entry)
    {
        // 현재 장면 — 미확정이니 되감는다. 롤백 한 걸음과 같은 기전(표적만 다르다).
        if (_vnFeatures.RequestBacklogJump(entry))
        {
            ClosePanel();
            await _episodePlayer.RequestReplayAsync();
            return;
        }

        // 이전 장면 — 확정된 것은 되돌리지 않는다. 그 장면 기록을 물려받아 새 회차로 갈라진다(장면 루트부터).
        if (_saveCoordinator == null || _progressionLauncher == null)
            return;

        int sceneIndex = _saveCoordinator.FindSceneIndexBySerial(entry.lineSerial);

        if (sceneIndex < 0)
            return;

        ClosePanel();

        await _progressionLauncher.StopAsync();
        _saveCoordinator.ForkFromScene(sceneIndex);
        await _progressionLauncher.LaunchAsync();
    }
}
