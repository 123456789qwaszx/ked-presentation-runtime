using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VnPlaybackSettings
{
    public float speedupModeMultiplier = 12f;
    public float autoModeDelaySeconds = 1.5f;

    public float userAdvanceCooldownSec = 0.13f;
    public float autoAdvanceRateLimitSec = 0.13f;

    public float cooldownAfterHurryUpSec = 0.18f;
    public float cooldownAfterNextLineSec = 0.1f; // Prevent double skip
}

public sealed class VnFeatureController : MonoBehaviour
{
    private VnUxState _vnUxState;
    [SerializeField] private VnPlaybackSettings _vnPlaybackSettings;

    private PresentationSessionContext _sessionContext;

    private EllipsisBreathTypewriter _typewriter;
    private VNLinePresentationState _linePresentationAdvanceState;
    private InlineEventMarkupHandler _inlineEventMarkupHandler;

    private BacklogRecorder _backlogRecorder;
    private AutoAdvanceScheduler _autoAdvanceScheduler;
    private FastForwardController _holdSpeedUpController;
    private RollbackHistory _rollbackController;
    private VNLinePresentationState _vnLinePresentationState;
    private ChoiceHistory _choiceHistory;

    private bool _speedUpToggled;
    private bool _speedUpHeld;

    public bool IsAuto => _sessionContext != null && _sessionContext.IsAutoMode;
    public bool IsSpeedup => _sessionContext != null && _sessionContext.IsSpeedUpMode;

    private bool LineFullyShown => _linePresentationAdvanceState.IsLineFullyShown;

    public IReadOnlyList<DialogueLogEntry> Backlogs => _backlogRecorder.Entries;

    private bool _init;

    public void Initialize(
        VnUxState uxState,
        VnPlaybackSettings vnPlaybackSettings,
        PresentationSessionContext sessionContext,
        VNLinePresentationState yarnLineLifecycleBridge,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        InlineEventMarkupHandler inlineEventMarkupHandler,
        BacklogRecorder backlogRecorder,
        AutoAdvanceScheduler autoAdvanceScheduler,
        FastForwardController holdSpeedUpController,
        RollbackHistory rollbackController,
        VNLinePresentationState vnLinePresentationState,
        ChoiceHistory choiceHistory)
    {
        if (_init)
            return;

        _vnUxState = uxState;
        _vnPlaybackSettings = vnPlaybackSettings;
        _sessionContext = sessionContext;
        _linePresentationAdvanceState = yarnLineLifecycleBridge;
        _typewriter = ellipsisBreathTypewriter;
        _inlineEventMarkupHandler = inlineEventMarkupHandler;

        _backlogRecorder = backlogRecorder;
        _autoAdvanceScheduler = autoAdvanceScheduler;
        _holdSpeedUpController = holdSpeedUpController;
        _rollbackController = rollbackController;
        _vnLinePresentationState = vnLinePresentationState;
        _choiceHistory = choiceHistory;

        _speedUpToggled = false;
        _speedUpHeld = false;

        _init = true;
    }

    private void Update()
    {
        if (!_init)
            return;

        if (_vnUxState.BacklogVisible)
            return;

        if (_vnUxState.ChoicesVisible)
            return;

        if (IsAuto && LineFullyShown)
            _autoAdvanceScheduler.Tick();

        _holdSpeedUpController.Tick();
    }

    public void ToggleAuto()
    {
        if (!_init)
            return;

        bool next = !IsAuto;

        _sessionContext.SetAutoModeEnabled(next);

        if (next && LineFullyShown)
            _autoAdvanceScheduler.ResetAutoAdvanceTimer();
    }

    public void TogglePlaybackSpeed()
    {
        if (!_init)
            return;

        _speedUpToggled = !_speedUpToggled;
        ApplySpeedUpState();
    }

    public void BeginFastForward()
    {
        if (!_init)
            return;

        _speedUpHeld = true;

        _holdSpeedUpController.SetHeld(true);
        _inlineEventMarkupHandler.SetPauseIgnored(true);

        ApplySpeedUpState();
    }

    public void EndFastForward()
    {
        if (!_init)
            return;

        _speedUpHeld = false;

        _holdSpeedUpController.SetHeld(false);
        _inlineEventMarkupHandler.SetPauseIgnored(false);

        ApplySpeedUpState();
    }

    public bool RequestRollbackOneStep()
    {
        if (!_init)
            return false;

        if (_vnLinePresentationState.IsSeekingActive)
            return false;

        if (!_rollbackController.GetRollbackPoint(out RollbackPoint target))
            return false;

        _choiceHistory.RemoveChoiceAnchorAfterRollbackPoint(target);

        _rollbackController.ClearRollbackPoints();
        _vnLinePresentationState.BeginRollbackSeek(target.nodeName, target.lineId);

        DisableAutoAndSpeedUpForSeek();

        _autoAdvanceScheduler.ResetAutoAdvanceTimer();

        return true;
    }

    private void DisableAutoAndSpeedUpForSeek()
    {
        _sessionContext.SetAutoModeEnabled(false);

        _speedUpToggled = false;
        _speedUpHeld = false;

        _holdSpeedUpController.SetHeld(false);
        _inlineEventMarkupHandler.SetPauseIgnored(false);

        ApplySpeedUpState();
    }

    private void ApplySpeedUpState()
    {
        bool speedUp = _speedUpToggled || _speedUpHeld;

        _sessionContext.SetSpeedUpModeEnabled(speedUp);

        float multiplier = speedUp
            ? _vnPlaybackSettings.speedupModeMultiplier
            : 1f;

        _typewriter.SetSpeedMultiplier(multiplier);
    }
}