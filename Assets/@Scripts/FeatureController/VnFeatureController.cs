using System.Collections.Generic;
using UnityEngine;

public sealed class VnFeatureController
{
    private VnPlaybackRuntimeState _vnPlaybackSettings;

    private EllipsisBreathTypewriter _typewriter;
    private VNLinePresentationState _linePresentationAdvanceState;

    private BacklogRecorder _backlogRecorder;
    private AutoAdvanceScheduler _autoAdvanceScheduler;
    private RapidSkipController _rapidSkipController;
    private RollbackHistory _rollbackController;
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
        VNLinePresentationState linePresentationAdvanceState,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        BacklogRecorder backlogRecorder,
        AutoAdvanceScheduler autoAdvanceScheduler,
        RapidSkipController rapidSkipController,
        RollbackHistory rollbackController,
        ChoiceHistory choiceHistory)
    {
        if (_init)
            return;

        _vnPlaybackSettings = vnPlaybackSettings;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _typewriter = ellipsisBreathTypewriter;

        _backlogRecorder = backlogRecorder;
        _autoAdvanceScheduler = autoAdvanceScheduler;
        _rapidSkipController = rapidSkipController;
        _rollbackController = rollbackController;
        _choiceHistory = choiceHistory;

        _speedUpToggled = false;
        _speedUpHeld = false;
        _rapidSkipHeld = false;

        ApplySpeedUpModeState();
        ApplyRapidSkipState();

        _init = true;
    }

    public void Tick()
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
        if (_linePresentationAdvanceState.IsSeekingActive)
            return false;

        if (!_rollbackController.GetRollbackPoint(out RollbackPoint target))
            return false;

        _choiceHistory.RemoveChoiceAnchorAfterRollbackPoint(target);

        _rollbackController.ClearRollbackPoints();
        _linePresentationAdvanceState.BeginRollbackSeek(target.nodeName, target.lineId);

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