using UnityEngine;

// VN 재생의 유일한 프레임 구동자.
public sealed class VnAdvanceInputPoller : MonoBehaviour
{
    [SerializeField] private VnAdvanceInputBindings _bindings = new();

    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private VnFeatureController _featureController;
    
    private VNLinePresentationState _linePresentationAdvanceState;
    private EpisodePlayer _episodePlayer;

    // 디버그 키(2번)로 재생할 노드. 이 키를 아는 건 여기뿐이다 — EpisodePlayer는 몰라도 된다.
    private string _yarnEntryKey;

    private bool _rapidSkipHeld;
    private bool _speedUpHeld;

    public void Initialize(
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VnFeatureController featureController,
        VNLinePresentationState linePresentationAdvanceState,
        EpisodePlayer episodePlayer,
        string yarnEntryKey)
    {
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _featureController = featureController;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _episodePlayer = episodePlayer;
        _yarnEntryKey = yarnEntryKey;
    }

    private void Update()
    {
        if (_dialogueAdvanceDispatcher == null || _featureController == null)
            return;

        // 입력을 먼저 반영한 뒤 그 결과로 틱을 돌림.
        PollAdvance();
        PollRapidSkip();
        PollSpeedUpMode();
        PollFeatureToggles();
        PollDebugRunYarn();

        _featureController.Tick();
    }

    // Update에서 부르는 입력 핸들러라 async void다. 첫 await에서 바로 반환된다.
    private async void PollDebugRunYarn()
    {
        if (_bindings.IsRunYarnPressed())
            await _episodePlayer.StartGameAsync(_yarnEntryKey);
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

    private async void PollFeatureToggles()
    {
        if (_bindings.IsAutoTogglePressed())
            _featureController.ToggleAuto();

        if (_bindings.IsRollbackPressed())
        {
            if (!_featureController.RequestRollbackOneStep())
                return;

            await _episodePlayer.StartGameAsync(_linePresentationAdvanceState.SeekTargetNodeName);
        }
    }
}