public sealed partial class VNScreenBindings
{
    private VNFeatureController _vnFeatures;
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private VNLinePresentationState _linePresentationAdvanceState;
    
    public void ConfigurePresentationView(
        VNFeatureController vnFeatures,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VNLinePresentationState linePresentationAdvanceState)
    {
        _vnFeatures = vnFeatures;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _linePresentationAdvanceState = linePresentationAdvanceState;
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

        await _episodePlayer.StartGameAsync(_linePresentationAdvanceState.SeekTargetNodeName);
    }
}