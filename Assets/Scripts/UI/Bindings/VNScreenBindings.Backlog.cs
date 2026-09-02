public sealed partial class VNScreenBindings
{
    private void OpenBacklogPanel()
    {
        UI.PushPanel<BacklogPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            panel.Present(_vnFeatures.Backlogs, entry => _vnFeatures.CanJumpTo(entry));
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

    // 백점프 — 롤백 한 걸음과 같은 기전(표적만 다르다). 패널을 접고 리플레이.
    private async void HandleBacklogJump(DialogueLogEntry entry)
    {
        if (!_vnFeatures.RequestBacklogJump(entry))
            return;

        ClosePanel();

        await _episodePlayer.RequestReplayAsync();
    }
}
