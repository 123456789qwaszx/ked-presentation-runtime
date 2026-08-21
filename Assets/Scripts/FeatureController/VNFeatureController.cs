using System.Collections.Generic;
using UnityEngine;

public sealed class VNFeatureController
{
    private readonly VNPlaybackRuntimeState _vnPlaybackSettings;

    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly VNLinePresentationState _linePresentationAdvanceState;

    private readonly BacklogRecorder _backlogRecorder;
    private readonly AutoAdvanceScheduler _autoAdvanceScheduler;
    private readonly RapidSkipController _rapidSkipController;
    private readonly RollbackHistory _rollbackController;
    private readonly ChoiceHistory _choiceHistory;

    private bool _speedUpToggled;
    private bool _speedUpHeld;
    private bool _rapidSkipHeld;

    private bool IsAuto => _vnPlaybackSettings.IsAutoMode;

    private bool LineFullyShown => _linePresentationAdvanceState.IsLineFullyShown;

    public IReadOnlyList<DialogueLogEntry> Backlogs => _backlogRecorder.Entries;

    public VNFeatureController(
        VNPlaybackRuntimeState vnPlaybackSettings,
        VNLinePresentationState linePresentationAdvanceState,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        BacklogRecorder backlogRecorder,
        AutoAdvanceScheduler autoAdvanceScheduler,
        RapidSkipController rapidSkipController,
        RollbackHistory rollbackController,
        ChoiceHistory choiceHistory)
    {
        _vnPlaybackSettings = vnPlaybackSettings;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _typewriter = ellipsisBreathTypewriter;

        _backlogRecorder = backlogRecorder;
        _autoAdvanceScheduler = autoAdvanceScheduler;
        _rapidSkipController = rapidSkipController;
        _rollbackController = rollbackController;
        _choiceHistory = choiceHistory;

        ApplySpeedUpModeState();
        ApplyRapidSkipState();
    }

    public void Tick()
    {
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

        // 백로그는 세션 연속 — 지우고 다시 쌓는 게 아니라 표적 뒤 꼬리만 걷는다.
        // 리플레이 패스스루는 백로그를 다시 적지 않으므로(VNYarnLineBoundary)
        // 표적까지의 기록이 그대로 남는다.
        _backlogRecorder.TruncateFromEnd(_rollbackController.CountPointsAfter(target));

        _rollbackController.ClearRollbackPoints();
        _linePresentationAdvanceState.BeginRollbackSeek(target.nodeName, target.lineId, target.occurrence);

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