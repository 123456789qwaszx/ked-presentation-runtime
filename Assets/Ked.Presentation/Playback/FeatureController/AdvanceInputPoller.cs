using System;
using UnityEngine;

// VN 재생의 유일한 프레임 입력 구동자.
// UI보다 늦게 돈다 — 같은 키로 UI가 메뉴를 연 프레임이면 InputBlocked가 이미 그것을 본다.
[DefaultExecutionOrder(100)]
public sealed class AdvanceInputPoller : MonoBehaviour
{
    // 화면(UI)이 입력을 가진 동안 true. 막힌 프레임은 폴링과 Tick(자동 · 빨리 넘기기)을 통째로 건너뛴다.
    public Func<bool> InputBlocked { get; set; }

    [SerializeField] private AdvanceInputBindings _bindings = new();

    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private PlaybackFeatureController _featureController;
    private IPlaybackHost _playbackHost;

    private bool _rapidSkipHeld;
    private bool _speedUpHeld;

    public void Initialize(
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        PlaybackFeatureController featureController,
        IPlaybackHost playbackHost)
    {
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _featureController = featureController;
        _playbackHost = playbackHost;
    }

    private void Update()
    {
        if (InputBlocked != null && InputBlocked())
            return;

        PollAdvance();
        PollRapidSkip();
        PollSpeedUpMode();
        PollFeatureToggles();

        _featureController?.Tick();
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

        if (!_bindings.IsRollbackPressed())
            return;

        // 다시 돌릴 쪽이 없으면 표적도 정하지 않는다 — 시크만 걸려 남는 상태를 만들지 않는다.
        if (!_playbackHost.IsRunning)
            return;

        if (!_featureController.RequestRollbackOneStep())
            return;

        await _playbackHost.RequestReplayAsync();
    }
}
