public sealed partial class VNScreenBindings
{
    public void GoToPresentationView()
    {
        UI.SwitchRoot<PresentationUIRoot>(
            afterPatched: root =>
            {
                BindView(root, ApplyBindings);

                // 이 실행이 갖지 않은 기능의 메뉴는 숨긴다 — 눌러도 아무 일 없는 버튼을 두지 않는다.
                root.SetProgressionMenusVisible(HasProgression);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(PresentationUIRoot root)
    {
        AddBinding(root,
            r => r.AutoClicked += HandleAutoClicked,
            r => r.AutoClicked -= HandleAutoClicked);

        AddBinding(root,
            r => r.BackLogClicked += HandleBackLogClicked,
            r => r.BackLogClicked -= HandleBackLogClicked);

        AddBinding(root,
            r => r.RapidSkipDown += HandleFastForwardDown,
            r => r.RapidSkipDown -= HandleFastForwardDown);

        AddBinding(root,
            r => r.RapidSkipUp += HandleFastForwardUp,
            r => r.RapidSkipUp -= HandleFastForwardUp);

        AddBinding(root,
            r => r.StepNextClicked += HandleHurryUpClicked,
            r => r.StepNextClicked -= HandleHurryUpClicked);

        AddBinding(root,
            r => r.PlaybackSpeedClicked += HandlePlaybackSpeedClicked,
            r => r.PlaybackSpeedClicked -= HandlePlaybackSpeedClicked);

        AddBinding(root,
            r => r.RollbackClicked += HandleRollbackClicked,
            r => r.RollbackClicked -= HandleRollbackClicked);

        AddBinding(root,
            r => r.SaveMenuClicked += HandleSaveMenuClicked,
            r => r.SaveMenuClicked -= HandleSaveMenuClicked);

        AddBinding(root,
            r => r.LoadMenuClicked += HandleLoadMenuClicked,
            r => r.LoadMenuClicked -= HandleLoadMenuClicked);
        
        AddBinding(root,
            r => r.OpenSkipPanelClicked += HandleOpenSkipPanelClicked,
            r => r.OpenSkipPanelClicked -= HandleOpenSkipPanelClicked);

        AddBinding(root,
            r => r.GoToTitleClicked += HandleGoToTitleClicked,
            r => r.GoToTitleClicked -= HandleGoToTitleClicked);
    }

    private void HandleAutoClicked()
    {
        _vnFeatures.ToggleAuto();
    }

    private void HandleBackLogClicked()
    {
        OpenBacklogPanel();
    }

    private void HandleFastForwardDown()
    {
        _vnFeatures.BeginRapidSkip();
    }

    private void HandleFastForwardUp()
    {
        _vnFeatures.EndRapidSkip();
    }

    private void HandleHurryUpClicked()
    {
        _dialogueAdvanceDispatcher.DispatchAdvance();
    }

    private void HandlePlaybackSpeedClicked()
    {
        _vnFeatures.ToggleSpeedUpMode();
    }

    private async void HandleRollbackClicked()
    {
        if (!_playbackHost.IsRunning)
            return;

        if (!_vnFeatures.RequestRollbackOneStep())
            return;

        await _playbackHost.RequestReplayAsync();
    }
    
    private void HandleSaveMenuClicked()
    {
        OpenSaveMenu();
    }

    private void HandleLoadMenuClicked()
    {
        OpenLoadMenu();
    }
    
    private void HandleOpenSkipPanelClicked()
    {
        OpenSkipConfirmPanel();
    }

    private void HandleGoToTitleClicked()
    {
        OpenGoToTitleConfirmPanel();
    }
}