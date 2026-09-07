public sealed partial class VNScreenBindings
{
    private VNFeatureController _vnFeatures;
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    
    public void ConfigurePresentationView(
        VNFeatureController vnFeatures,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher)
    {
        _vnFeatures = vnFeatures;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
    }

    public void GoToPresentationView()
    {
        UI.SwitchRoot<PresentationUIRoot>(root =>
        {
            BindMain(root, ApplyBindings);
        });
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
        if (!_vnFeatures.RequestRollbackOneStep())
            return;

        await _progressionLauncher.RequestReplayAsync();
    }
    
    private void HandleSaveMenuClicked()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    private void HandleLoadMenuClicked()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }
    
    private void HandleOpenSkipPanelClicked()
    {
        OpenSkipConfirmPanel();
    }
}