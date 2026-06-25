using UnityEngine;

public sealed class VnAdvanceInputPoller : MonoBehaviour
{
    [SerializeField] private VnAdvanceInputBindings _bindings = new();

    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private VnFeatureController _featureController;
    
    private VNLinePresentationState _linePresentationAdvanceState;
    private EpisodePlayer _episodePlayer;

    private bool _rapidSkipHeld;
    private bool _speedUpHeld;

    public void Initialize(
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VnFeatureController featureController,
        VnAdvanceInputBindings bindings,
        VNLinePresentationState linePresentationAdvanceState,
        EpisodePlayer episodePlayer)
    {
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _featureController = featureController;

        if (bindings != null)
            _bindings = bindings;
        
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _episodePlayer = episodePlayer;
    }

    private void Update()
    {
        if (_dialogueAdvanceDispatcher == null || _featureController == null)
            return;

        PollAdvance();
        PollRapidSkip();
        PollSpeedUpMode();
        PollFeatureToggles();
    }

    private void PollAdvance()
    {
        if (_bindings.IsAdvancePressed())
            _dialogueAdvanceDispatcher.DispatchAdvance();
    }

    private void PollRapidSkip()
    {
        bool held = _bindings.IsRapidSkipHeld();

        if (held && !_rapidSkipHeld)
            _featureController.BeginRapidSkip();

        if (!held && _rapidSkipHeld)
            _featureController.EndRapidSkip();

        _rapidSkipHeld = held;
    }

    private void PollSpeedUpMode()
    {
        bool held = _bindings.IsSpeedUpHeld();

        if (held && !_speedUpHeld)
            _featureController.BeginSpeedUpMode();

        if (!held && _speedUpHeld)
            _featureController.EndSpeedUpMode();

        _speedUpHeld = held;

        if (_bindings.IsSpeedUpTogglePressed())
            _featureController.ToggleSpeedUpMode();
    }

    private void PollFeatureToggles()
    {
        if (_bindings.IsAutoTogglePressed())
            _featureController.ToggleAuto();

        if (_bindings.IsRollbackPressed())
        {
            if (!_featureController.RequestRollbackOneStep())
                return;
            
            _episodePlayer.StartGame(_linePresentationAdvanceState.SeekTargetNodeName);
        }
    }
}