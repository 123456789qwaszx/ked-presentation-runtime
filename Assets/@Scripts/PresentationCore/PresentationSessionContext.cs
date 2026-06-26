using System;

[Serializable]
public sealed class PresentationSessionContext
{
    private readonly PresentationPlaybackSettings _playback = new();

    private bool _isStepCommandBusy;
    private bool _isBlockingInput;
    private bool _closeRequested;

    public bool IsStepCommandBusy => _isStepCommandBusy;
    public bool IsBlockingInput => _isBlockingInput;
    public bool CloseRequested => _closeRequested;

    public bool IsAutoMode => _playback.IsAutoMode;
    public bool IsSpeedUpMode => _playback.IsSpeedUpMode;
    public bool IsRapidSkipMode => _playback.IsRapidSkipMode;

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

    public void SetRapidSkipModeEnabled(bool enabled)
    {
        _playback.SetRapidSkipModeEnabled(enabled);
    }

    public void SetTimeScale(float timeScale)
    {
        _playback.TimeScale = timeScale;
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

    public void EnterRapidSkip()
    {
        SetRapidSkipModeEnabled(true);
    }

    public void ExitRapidSkip()
    {
        SetRapidSkipModeEnabled(false);
    }

    /// <summary>
    /// Must be called only by the CommandRunScope to toggle busy state.
    /// </summary>
    public void SetNodeBusy(bool busy)
    {
        _isStepCommandBusy = busy;
    }

    public void RequestClose()
    {
        _closeRequested = true;
    }

    public void ResetSessionFlagsForStart()
    {
        _isStepCommandBusy = false;
        _isBlockingInput = false;
        _closeRequested = false;

        _playback.ResetDefaults();
    }
}