using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VnPlaybackSettings
{
    public int maxLogCount = 100;
    public float speedupModeMultiplier = 12f;
    public float autoModeDelaySeconds = 1.5f;
    
    public float userAdvanceCooldownSec = 0.13f; // 130ms
    public float autoAdvanceRateLimitSec = 0.13f; // 130ms
    
    public float cooldownAfterHurryUpSec = 0.18f;
    public float cooldownAfterNextLineSec = 0.1f;//Mathf.Max(0.12f, 0.1f); // Prevent double-skip: enforce a minimum cooldown
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
    private RollbackController _rollbackController;

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
        RollbackController rollbackController)
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
        if (IsAuto)
            SetMode(VnPlayMode.Manual);
        else
            SetMode(VnPlayMode.Auto);
    }

    public void TogglePlaybackSpeed()
    {
        if (IsSpeedup)
            SetMode(VnPlayMode.Manual);
        else
            SetMode(VnPlayMode.Speedup);
    }

    public void BeginFastForward()
    {
        if (!_init)
            return;

        _holdSpeedUpController.SetHeld(true);
        _inlineEventMarkupHandler.SetPauseIgnored(true);
        RefreshPlaybackSpeed();
    }

    public void EndFastForward()
    {
        if (!_init)
            return;

        _holdSpeedUpController.SetHeld(false);
        _inlineEventMarkupHandler.SetPauseIgnored(false);
        RefreshPlaybackSpeed();
    }

    public bool RequestRollbackOneStep()
    {
        SetMode(VnPlayMode.Manual);
        _autoAdvanceScheduler.ResetAutoAdvanceTimer();

        return _rollbackController.RequestRollbackOneStep();
    }

    private void SetMode(VnPlayMode mode)
    {
        _sessionContext.SetPlayMode(mode);
        ApplyModeSideEffects(mode);
    }

    private void RefreshPlaybackSpeed()
    {
        bool shouldSpeedUp = _sessionContext.IsSpeedUpMode;

        float multiplier = shouldSpeedUp
            ? _vnPlaybackSettings.speedupModeMultiplier
            : 1f;

        _typewriter.SetSpeedMultiplier(multiplier);
    }

    private void ApplyModeSideEffects(VnPlayMode current)
    {
        if (current == VnPlayMode.Auto && LineFullyShown)
            _autoAdvanceScheduler.ResetAutoAdvanceTimer();

        float mul = current == VnPlayMode.Speedup
            ? _vnPlaybackSettings.speedupModeMultiplier
            : 1f;

        _typewriter.SetSpeedMultiplier(mul);
    }
}