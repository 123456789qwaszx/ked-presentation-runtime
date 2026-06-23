public sealed partial class VnScreenBindings
{
    private VnFeatureController _vnFeatures;
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private VNLinePresentationState _linePresentationAdvanceState;
    
    public void ConfigurePresentationView(
        VnFeatureController vnFeatures,
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
            r => r.FastForwardDown += HandleFastForwardDown,
            r => r.FastForwardDown -= HandleFastForwardDown);
        
        AddBinding(root,
            r => r.FastForwardUp += HandleFastForwardUp,
            r => r.FastForwardUp -= HandleFastForwardUp);
        
        AddBinding(root,
            r => r.ExpandClicked += HandleExpandClicked,
            r => r.ExpandClicked -= HandleExpandClicked);
        
        AddBinding(root,
            r => r.HurryUpClicked += HandleHurryUpClicked,
            r => r.HurryUpClicked -= HandleHurryUpClicked);
        
        AddBinding(root,
            r => r.LoadMenuClicked += HandleLoadMenuClicked,
            r => r.LoadMenuClicked -= HandleLoadMenuClicked);
        
        AddBinding(root,
            r => r.PlaybackSpeedClicked += HandlePlaybackSpeedClicked,
            r => r.PlaybackSpeedClicked -= HandlePlaybackSpeedClicked);
        
        AddBinding(root,
            r => r.QuickMenuClicked += HandleQuickMenuClicked,
            r => r.QuickMenuClicked -= HandleQuickMenuClicked);
        
        AddBinding(root, 
            r => r.RollbackClicked += HandleRollbackClicked, 
            r => r.RollbackClicked -= HandleRollbackClicked);
        
        AddBinding(root,
            r => r.SaveMenuClicked += HandleSaveMenuClicked,
            r => r.SaveMenuClicked -= HandleSaveMenuClicked);
        
        AddBinding(root,
            r => r.SkipMenuClicked += HandleSkipMenuClicked,
            r => r.SkipMenuClicked -= HandleSkipMenuClicked);
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
        _vnFeatures.BeginFastForward();
    }

    private void HandleFastForwardUp()
    {
        _vnFeatures.EndFastForward();
    }

    private void HandleHurryUpClicked()
    {
        _dialogueAdvanceDispatcher.DispatchAdvance();
    }

    private void HandleLoadMenuClicked()
    {
        if (HasPanel)
            return;

        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void HandlePlaybackSpeedClicked()
    {
        _vnFeatures.TogglePlaybackSpeed();
    }

    private void HandleRollbackClicked()
    {
        if (!_vnFeatures.RequestRollbackOneStep())
            return;

        _episodePlayer.StartGame(_linePresentationAdvanceState.SeekTargetNodeName);
    }
    
    private void HandleQuickMenuClicked()
    {
        
    }

    private void HandleExpandClicked()
    {
        
    }

    private void HandleSaveMenuClicked()
    {
        if (HasPanel)
            return;

        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    private void HandleSkipMenuClicked()
    {
        OpenEpisodeSkipConfirmPanel();
    }
}