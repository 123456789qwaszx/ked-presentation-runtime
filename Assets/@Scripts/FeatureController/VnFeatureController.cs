using System.Collections.Generic;
using UnityEngine;

public sealed class VnFeatureController : MonoBehaviour
{
    private VnPlaybackRuntimeState _vnPlaybackSettings;

    private EllipsisBreathTypewriter _typewriter;
    private VNLinePresentationState _linePresentationAdvanceState;

    private BacklogRecorder _backlogRecorder;
    private AutoAdvanceScheduler _autoAdvanceScheduler;
    private RapidSkipController _rapidSkipController;
    private RollbackHistory _rollbackController;
    private VNLinePresentationState _vnLinePresentationState;
    private ChoiceHistory _choiceHistory;

    private bool _speedUpToggled;
    private bool _speedUpHeld;
    private bool _rapidSkipHeld;

    private bool IsAuto => _vnPlaybackSettings.IsAutoMode;

    private bool LineFullyShown => _linePresentationAdvanceState.IsLineFullyShown;

    public IReadOnlyList<DialogueLogEntry> Backlogs => _backlogRecorder.Entries;

    private bool _init;

    public void Initialize(
        VnPlaybackRuntimeState vnPlaybackSettings,
        VNLinePresentationState yarnLineLifecycleBridge,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        BacklogRecorder backlogRecorder,
        AutoAdvanceScheduler autoAdvanceScheduler,
        RapidSkipController rapidSkipController,
        RollbackHistory rollbackController,
        VNLinePresentationState vnLinePresentationState,
        ChoiceHistory choiceHistory)
    {
        if (_init)
            return;

        _vnPlaybackSettings = vnPlaybackSettings;
        _linePresentationAdvanceState = yarnLineLifecycleBridge;
        _typewriter = ellipsisBreathTypewriter;

        _backlogRecorder = backlogRecorder;
        _autoAdvanceScheduler = autoAdvanceScheduler;
        _rapidSkipController = rapidSkipController;
        _rollbackController = rollbackController;
        _vnLinePresentationState = vnLinePresentationState;
        _choiceHistory = choiceHistory;

        _speedUpToggled = false;
        _speedUpHeld = false;
        _rapidSkipHeld = false;

        ApplySpeedUpModeState();
        ApplyRapidSkipState();

        _init = true;
    }

    private void Update()
    {
        if (!_init)
            return;

        if (IsAuto && LineFullyShown)
            _autoAdvanceScheduler.Tick();

        if (_vnPlaybackSettings.IsSpeedUpMode && LineFullyShown)
            _autoAdvanceScheduler.ResetAutoAdvanceTimer();

        _rapidSkipController.Tick();
    }

    public void ToggleAuto()
    {
        bool next = !IsAuto;

        _vnPlaybackSettings.SetAutoModeEnabled(next);

        if (next && LineFullyShown)
            _autoAdvanceScheduler.ResetAutoAdvanceTimer();
    }

    public void ToggleSpeedUpMode()
    {
        _speedUpToggled = !_speedUpToggled;
        ApplySpeedUpModeState();
    }

    public void BeginSpeedUpMode()
    {
        _speedUpHeld = true;
        ApplySpeedUpModeState();
    }

    public void EndSpeedUpMode()
    {
        _speedUpHeld = false;
        ApplySpeedUpModeState();
    }

    public void BeginRapidSkip()
    {
        _rapidSkipHeld = true;
        ApplyRapidSkipState();
    }

    public void EndRapidSkip()
    {
        _rapidSkipHeld = false;
        ApplyRapidSkipState();
    }

    public bool RequestRollbackOneStep()
    {
        if (_vnLinePresentationState.IsSeekingActive)
            return false;

        if (!_rollbackController.GetRollbackPoint(out RollbackPoint target))
            return false;

        _choiceHistory.RemoveChoiceAnchorAfterRollbackPoint(target);

        _rollbackController.ClearRollbackPoints();
        _vnLinePresentationState.BeginRollbackSeek(target.nodeName, target.lineId);

        DisablePlaybackModifiersForSeek();

        _autoAdvanceScheduler.ResetAutoAdvanceTimer();

        return true;
    }

    private void DisablePlaybackModifiersForSeek()
    {
        _vnPlaybackSettings.SetAutoModeEnabled(false);

        _speedUpToggled = false;
        _speedUpHeld = false;
        _rapidSkipHeld = false;

        _rapidSkipController.SetHeld(false);

        ApplySpeedUpModeState();
        ApplyRapidSkipState();
    }

    private void ApplySpeedUpModeState()
    {
        bool speedUp = _speedUpToggled || _speedUpHeld;

        float multiplier = 1f;

        if (speedUp)
        {
            multiplier = Mathf.Clamp(
                _vnPlaybackSettings.PlaybackSettings.speedupModeMultiplier,
                _vnPlaybackSettings.PlaybackSettings.speedupModeMinMultiplier,
                _vnPlaybackSettings.PlaybackSettings.speedupModeMaxMultiplier);
        }

        _vnPlaybackSettings.SetSpeedUpModeEnabled(speedUp);
        _vnPlaybackSettings.SetTimeScale(multiplier);

        _typewriter.SetSpeedMultiplier(multiplier);
    }

    private void ApplyRapidSkipState()
    {
        _vnPlaybackSettings.SetRapidSkipModeEnabled(_rapidSkipHeld);
        _rapidSkipController.SetHeld(_rapidSkipHeld);
    }
}