using System;

[Serializable]
public sealed class PresentationPlaybackSettings
{
    public const float DefaultTimeScale = 1f;
    public const float DefaultAutoAdvanceDelay = 0.6f;

    private float _timeScale = DefaultTimeScale;
    private float _autoAdvanceDelay = DefaultAutoAdvanceDelay;

    private bool _autoModeEnabled;
    private bool _speedUpModeEnabled;
    private bool _rapidSkipEnabled;

    public bool IsAutoMode => _autoModeEnabled;
    public bool IsSpeedUpMode => _speedUpModeEnabled;
    public bool IsRapidSkipMode => _rapidSkipEnabled;

    public float TimeScale
    {
        get => _timeScale;
        set => _timeScale = value < 1f ? 1f : value;
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

    public void SetRapidSkipModeEnabled(bool enabled)
    {
        _rapidSkipEnabled = enabled;
    }

    public void ResetDefaults()
    {
        _autoModeEnabled = false;
        _speedUpModeEnabled = false;
        _rapidSkipEnabled = false;

        _timeScale = DefaultTimeScale;
        _autoAdvanceDelay = DefaultAutoAdvanceDelay;
    }
}
