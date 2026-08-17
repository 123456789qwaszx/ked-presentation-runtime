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

    // PresentationUIRoot는 아직 QuickMenu · Expand · Save · Load · OpenSkipPanel 이벤트도 내보내지만
    // 그 기능들(세이브로드 · 에피소드 스킵)이 없어져서 붙일 핸들러가 없다.
    // 버튼은 프리팹에 남아 있고, 눌러도 구독자가 없어 아무 일도 일어나지 않는다.
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

    // UI 이벤트 핸들러라 async void다.
    private async void HandleRollbackClicked()
    {
        if (!_vnFeatures.RequestRollbackOneStep())
            return;

        await _episodePlayer.StartGameAsync(_linePresentationAdvanceState.SeekTargetNodeName);
    }
}