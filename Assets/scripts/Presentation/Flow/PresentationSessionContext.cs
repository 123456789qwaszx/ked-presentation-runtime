using System;

[Serializable]
public sealed class PresentationPlaybackSettings
{
    public const float DefaultTimeScale        = 1f;
    public const float DefaultAutoAdvanceDelay = 0.6f;
    
    private float _timeScale = DefaultTimeScale;
    private float _autoAdvanceDelay = DefaultAutoAdvanceDelay;

    public bool IsAutoMode  { get; set; }
    public bool IsSkipping  { get; set; }
    
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
        IsAutoMode        = false;
        IsSkipping        = false;
        _timeScale        = DefaultTimeScale;
        _autoAdvanceDelay = DefaultAutoAdvanceDelay;
    }
    
    public bool enableDebugStart;
    public string debugStartStepName;
}

[Serializable]
public sealed class PresentationSessionContext
{
    private readonly PresentationPlaybackSettings _playback;
    
    private bool _isNodeBusy;
    private bool _isBlockingInput;
    private bool _closeRequested;
    
    public PresentationSessionContext(PresentationPlaybackSettings playback)
    {
        _playback = playback;
    }

    public bool IsNodeBusy => _isNodeBusy;
    public bool IsBlockingInput => _isBlockingInput;
    public bool CloseRequested => _closeRequested;
    
    public bool IsAutoMode => _playback.IsAutoMode;
    public bool IsSkipping => _playback.IsSkipping;
    public float TimeScale => _playback.TimeScale;
    public float AutoAdvanceDelay => _playback.AutoAdvanceDelay;
    
    
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
        _isNodeBusy      = false;
        _isBlockingInput = false;
        _closeRequested  = false;
    }

    public bool IsDebugStartEnabled => _playback.enableDebugStart;
    public string DebugStartStepName => _playback.debugStartStepName;
}