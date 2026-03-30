using System;
using System.Collections.Generic;
using UnityEngine;

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
    
    public float cooldownAfterHurryUpSec = 0.28f;
    public float cooldownAfterNextLineSec = Mathf.Max(0.12f, 0.1f); // Prevent double-skip: enforce a minimum cooldown

    public VnPlayMode vnPlayMode = VnPlayMode.Manual;
    
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
    
    private BacklogRecorder _backlogRecorder;
    private AutoAdvanceScheduler _autoAdvanceScheduler;
    
    public bool IsAuto => _vnPlaybackSettings.IsAuto;
    public bool IsSpeedup => _vnPlaybackSettings.IsSpeedup;
    private bool LineFullyShown => _yarnLineLifecycleBridge.IsLineFullyShown;
    
    public IReadOnlyList<DialogueLogEntry> Backlogs => _backlogRecorder.Entries;
    
    private bool _init;

    public void Initialize(
        VnUxState uxState,
        VnPlaybackSettings vnPlaybackSettings,
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        BacklogRecorder backlogRecorder,
        AutoAdvanceScheduler autoAdvanceScheduler
       )
    {
        if (_init) return;
        
        _vnUxState = uxState;
        _vnPlaybackSettings = vnPlaybackSettings;
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;
        _typewriter = ellipsisBreathTypewriter;
        
        _backlogRecorder = backlogRecorder;
        _autoAdvanceScheduler = autoAdvanceScheduler;
        
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
    }

    public void ToggleAuto()
    {
        if (IsAuto) SetMode(VnPlayMode.Manual);
        else SetMode(VnPlayMode.Auto);
    }

    public void ToggleSpeedup()
    {
        if (IsSpeedup) SetMode(VnPlayMode.Manual);
        else SetMode(VnPlayMode.Speedup);
    }

    private void SetMode(VnPlayMode mode)
    {
        _vnPlaybackSettings.ChangePlayMode(mode);

        ApplyModeSideEffects(mode);
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