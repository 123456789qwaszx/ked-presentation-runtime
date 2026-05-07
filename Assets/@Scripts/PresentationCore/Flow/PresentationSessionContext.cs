using System;

public enum VnPlayMode
{
    Manual = 0,
    Auto = 1,
    Speedup = 2
}

[Serializable]
public sealed class PresentationPlaybackSettings
{
    public const float DefaultTimeScale = 1f;
    public const float DefaultAutoAdvanceDelay = 0.6f;

    private float _timeScale = DefaultTimeScale;
    private float _autoAdvanceDelay = DefaultAutoAdvanceDelay;

    private VnPlayMode _playMode = VnPlayMode.Manual;

    public VnPlayMode PlayMode
    {
        get => _playMode;
        set => _playMode = value;
    }

    public bool IsAutoMode => _playMode == VnPlayMode.Auto;
    public bool IsSkipping => _playMode == VnPlayMode.Speedup;

    public bool IsRollbackSeeking { get; set; }

    public float TimeScale
    {
        get => _timeScale;
        set => _timeScale = value < 0f ? 0f : value;
    }

    public float AutoAdvanceDelay
    {
        get => _autoAdvanceDelay;
        set => _autoAdvanceDelay = value > 0f ? value : DefaultAutoAdvanceDelay;
    }

    public void ResetDefaults()
    {
        _playMode = VnPlayMode.Manual;
        IsRollbackSeeking = false;
        _timeScale = DefaultTimeScale;
        _autoAdvanceDelay = DefaultAutoAdvanceDelay;
    }

    public bool enableDebugStart;
    public string debugStartStepName;
}

[Serializable]
public sealed class PresentationSessionContext
{
    private readonly PresentationPlaybackSettings _playback = new();

    private bool _isNodeBusy;
    private bool _isBlockingInput;
    private bool _closeRequested;

    public bool IsNodeBusy => _isNodeBusy;
    public bool IsBlockingInput => _isBlockingInput;
    public bool CloseRequested => _closeRequested;

    public VnPlayMode PlayMode => _playback.PlayMode;

    public bool IsAutoMode => _playback.IsAutoMode;
    public bool IsSkipping => _playback.IsSkipping;
    public bool IsRollbackSeeking => _playback.IsRollbackSeeking;

    public float TimeScale => _playback.TimeScale;
    public float AutoAdvanceDelay => _playback.AutoAdvanceDelay;
    
    private bool _suppressImmediateLineTransition;
    private string _suppressImmediateLineTransitionLineId;

    public void SetPlayMode(VnPlayMode mode)
    {
        _playback.PlayMode = mode;
    }

    public void EnterAutoMode()
    {
        _playback.PlayMode = VnPlayMode.Auto;
    }

    public void ExitAutoMode()
    {
        if (_playback.PlayMode == VnPlayMode.Auto)
            _playback.PlayMode = VnPlayMode.Manual;
    }

    public void EnterSpeedUpHeld()
    {
        _playback.PlayMode = VnPlayMode.Speedup;
    }

    public void ExitSpeedUpHeld()
    {
        if (_playback.PlayMode == VnPlayMode.Speedup)
            _playback.PlayMode = VnPlayMode.Manual;
    }

    public void EnterRollbackSeek()
    {
        _playback.IsRollbackSeeking = true;
    }

    public void ExitRollbackSeek()
    {
        _playback.IsRollbackSeeking = false;
    }

    /// <summary>
    /// Must be called only by the CommandRunScope to toggle busy state.
    /// </summary>
    public void SetNodeBusy(bool busy)
    {
        _isNodeBusy = busy;
    }

    public void RequestClose()
    {
        _closeRequested = true;
    }

    public void ResetSessionFlagsForStart()
    {
        _isNodeBusy = false;
        _isBlockingInput = false;
        _closeRequested = false;
        _playback.ResetDefaults();
    }

    public bool IsDebugStartEnabled => _playback.enableDebugStart;
    public string DebugStartStepName => _playback.debugStartStepName;
    
    
    private string _rollbackTargetLineId;
    private bool _isRollbackTargetLineReady;

    public void EnterRollbackSeek(string targetLineId)
    {
        _playback.IsRollbackSeeking = true;
        _rollbackTargetLineId = targetLineId;
        _isRollbackTargetLineReady = false;
    }

    public void MarkRollbackTargetLineReady()
    {
        _playback.IsRollbackSeeking = false;
        _isRollbackTargetLineReady = true;
    }

    public bool ConsumeRollbackTargetLine(string lineId)
    {
        if (!_isRollbackTargetLineReady)
            return false;

        if (_rollbackTargetLineId != lineId)
            return false;

        _isRollbackTargetLineReady = false;
        _rollbackTargetLineId = null;
        return true;
    }
}