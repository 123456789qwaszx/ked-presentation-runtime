using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum VnPlayMode
{
    Manual = 0,
    Auto = 1,
    Speedup = 2
}

[Serializable]
public class VnPlaybackSettings
{
    public int maxLogCount = 100;
    public float speedupModeMultiplier = 12f;
    public float autoModeDelaySeconds = 1.5f;
    
    public float userAdvanceCooldownSec = 0.13f; // 130ms
    public float autoAdvanceRateLimitSec = 0.13f; // 130ms
    
    public float cooldownAfterHurryUpSec = 0.18f;
    public float cooldownAfterNextLineSec = 0.01f;//Mathf.Max(0.12f, 0.1f); // Prevent double-skip: enforce a minimum cooldown

    public VnPlayMode vnPlayMode = VnPlayMode.Manual;
    public bool isSpeedUpToggleHeld = false;
    
    public void ChangePlayMode(VnPlayMode mode) => vnPlayMode =  mode;
    
    public bool IsAuto => vnPlayMode == VnPlayMode.Auto;
    public bool IsSpeedup => vnPlayMode == VnPlayMode.Speedup;
}

public sealed class VnFeatureController : MonoBehaviour
{
    private VnUxState _vnUxState;
    [SerializeField] private VnPlaybackSettings _vnPlaybackSettings;
    private EllipsisBreathTypewriter _typewriter;
    private YarnLineLifecycleBridge _yarnLineLifecycleBridge;
    private InlineEventMarkupHandler _inlineEventMarkupHandler;
    
    private BacklogRecorder _backlogRecorder;
    private AutoAdvanceScheduler _autoAdvanceScheduler;
    private HoldSpeedUpController _holdSpeedUpController;
    private RollbackController _rollbackController;
    
    public bool IsAuto => _vnPlaybackSettings.IsAuto;
    public bool IsSpeedup => _vnPlaybackSettings.IsSpeedup;
    public bool IsSkipHeld => _vnPlaybackSettings.isSpeedUpToggleHeld;
    
    private bool LineFullyShown => _yarnLineLifecycleBridge.IsLineFullyShown;
    
    public IReadOnlyList<DialogueLogEntry> Backlogs => _backlogRecorder.Entries;
    
    private bool _init;

    public void Initialize(
        VnUxState uxState,
        VnPlaybackSettings vnPlaybackSettings,
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        InlineEventMarkupHandler inlineEventMarkupHandler,
        BacklogRecorder backlogRecorder,
        AutoAdvanceScheduler autoAdvanceScheduler,
        HoldSpeedUpController holdSpeedUpController,
        RollbackController rollbackController
       )
    {
        if (_init) return;
        
        _vnUxState = uxState;
        _vnPlaybackSettings = vnPlaybackSettings;
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;
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
        {
            _autoAdvanceScheduler.Tick();
        }

        _holdSpeedUpController.Tick();
        
    }

    public void ToggleAuto()
    {
        if (IsAuto) SetMode(VnPlayMode.Manual);
        else SetMode(VnPlayMode.Auto);
    }

    public void ToggleSetSpeed()
    {
        if (IsSpeedup) SetMode(VnPlayMode.Manual);
        else SetMode(VnPlayMode.Speedup);
    }

    public void BeginHoldSpeedUp()
    {
        if (!_init)
            return;

        _holdSpeedUpController.SetHeld(true);
        _inlineEventMarkupHandler.SetPauseIgnored(true);
        RefreshPlaybackSpeed();
    }

    public void EndHoldSpeedUp()
    {
        if (!_init)
            return;

        _holdSpeedUpController.SetHeld(false);
        _inlineEventMarkupHandler.SetPauseIgnored(false);
        RefreshPlaybackSpeed();
    }

    public void RequestRollbackOneStep()
    {
        // if (!_rollbackController.CanRollback)
        //     return;
        
        SetMode(VnPlayMode.Manual);
        _autoAdvanceScheduler.ResetAutoAdvanceTimer();

        _rollbackController.RequestRollbackOneStep();
    }

    private void SetMode(VnPlayMode mode)
    {
        _vnPlaybackSettings.isSpeedUpToggleHeld = true;
        _vnPlaybackSettings.ChangePlayMode(mode);

        ApplyModeSideEffects(mode);
    }
    
    private void RefreshPlaybackSpeed()
    {
        bool shouldSpeedUp =
            _vnPlaybackSettings.IsSpeedup ||
            _vnPlaybackSettings.isSpeedUpToggleHeld;

        float multiplier = shouldSpeedUp
            ? _vnPlaybackSettings.speedupModeMultiplier
            : 1f;

        _typewriter.SetSpeedMultiplier(multiplier);
    }

    private void ApplyModeSideEffects(VnPlayMode current)
    {
        if (current == VnPlayMode.Auto && LineFullyShown)
            _autoAdvanceScheduler.ResetAutoAdvanceTimer();
        
        float mul = (current == VnPlayMode.Speedup) ?
            _vnPlaybackSettings.speedupModeMultiplier 
            : 1f;
        _typewriter.SetSpeedMultiplier(mul);
    }
}