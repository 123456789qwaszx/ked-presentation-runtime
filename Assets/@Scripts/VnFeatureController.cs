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
public class VnFeaturePolicy
{
    public VnPlayMode vnPlayMode = VnPlayMode.Manual;
    public int maxLogCount = 100;
    public float speedupMultiplier = 12f;
    public float autoDelaySeconds = 1.5f;

    public void ChangePlayMode(VnPlayMode mode) => vnPlayMode =  mode;
}

public sealed class VnFeatureController : MonoBehaviour
{
    private EllipsisBreathTypewriter _typewriter;
    private VnUxState _uxState;
    private BacklogRecorder _backlogRecorder;
    private AutoAdvanceScheduler _autoAdvanceScheduler;
    
    [Header("FeaturePolicy")]
    [SerializeField] private VnFeaturePolicy vnFeaturePolicy;

    public VnPlayMode Mode => vnFeaturePolicy.vnPlayMode;
    public bool IsAuto => Mode == VnPlayMode.Auto;
    public bool IsSpeedup => Mode == VnPlayMode.Speedup;
    public IReadOnlyList<DialogueLogEntry> Backlogs => _backlogRecorder.Entries;
    
    private bool _init;

    public void Initialize(
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        VnUxState uxState,
        BacklogRecorder backlogRecorder,
        AutoAdvanceScheduler autoAdvanceScheduler
       )
    {
        if (_init) return;
        
        _typewriter = ellipsisBreathTypewriter;
        _uxState = uxState;
        _backlogRecorder = backlogRecorder;
        _autoAdvanceScheduler = autoAdvanceScheduler;
        
        _init = true;
        ApplyModeSideEffects(VnPlayMode.Manual, Mode);
    }
    
    private void Update()
    {
        if (!_init)
            return;
        
        if (_uxState.BacklogVisible)
            return;
        
        if (_uxState.ChoicesVisible)
            return;
        
        if (Mode == VnPlayMode.Auto)
            _autoAdvanceScheduler?.Tick();
    }

    public void ToggleAuto()
    {
        if (Mode == VnPlayMode.Auto) SetMode(VnPlayMode.Manual);
        else SetMode(VnPlayMode.Auto);
    }

    public void ToggleSpeedup()
    {
        if (Mode == VnPlayMode.Speedup) SetMode(VnPlayMode.Manual);
        else SetMode(VnPlayMode.Speedup);
    }

    private void SetMode(VnPlayMode mode)
    {
        if (Mode == mode)
            return;

        VnPlayMode prev = Mode;
        vnFeaturePolicy.ChangePlayMode(mode);

        ApplyModeSideEffects(prev, Mode);
    }

    private void ApplyModeSideEffects(VnPlayMode previous, VnPlayMode current)
    {
        if (current == VnPlayMode.Auto)
            _autoAdvanceScheduler.SetEnabled(true);
        else _autoAdvanceScheduler.SetEnabled(false);
        
        float mul = (current == VnPlayMode.Speedup) ?
            vnFeaturePolicy.speedupMultiplier 
            : 1f;
        _typewriter.SetSpeedMultiplier(mul);
    }
}
