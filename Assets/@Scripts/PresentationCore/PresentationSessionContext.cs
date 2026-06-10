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

    private bool _autoModeEnabled;
    private bool _speedUpModeEnabled;

    public bool IsAutoMode => _autoModeEnabled;
    public bool IsSpeedUpMode => _speedUpModeEnabled;

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

    public void SetAutoModeEnabled(bool enabled)
    {
        _autoModeEnabled = enabled;
    }

    public void SetSpeedUpModeEnabled(bool enabled)
    {
        _speedUpModeEnabled = enabled;
    }

    public void ResetDefaults()
    {
        _autoModeEnabled = false;
        _speedUpModeEnabled = false;

        _timeScale = DefaultTimeScale;
        _autoAdvanceDelay = DefaultAutoAdvanceDelay;
    }
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

    public bool IsAutoMode => _playback.IsAutoMode;
    public bool IsSpeedUpMode => _playback.IsSpeedUpMode;

    public float TimeScale => _playback.TimeScale;
    public float AutoAdvanceDelay => _playback.AutoAdvanceDelay;

    public void SetAutoModeEnabled(bool enabled)
    {
        _playback.SetAutoModeEnabled(enabled);
    }

    public void SetSpeedUpModeEnabled(bool enabled)
    {
        _playback.SetSpeedUpModeEnabled(enabled);
    }

    public void EnterAutoMode()
    {
        SetAutoModeEnabled(true);
    }

    public void ExitAutoMode()
    {
        SetAutoModeEnabled(false);
    }

    public void EnterSpeedUp()
    {
        SetSpeedUpModeEnabled(true);
    }

    public void ExitSpeedUp()
    {
        SetSpeedUpModeEnabled(false);
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
}