using System;

[Serializable]
public sealed class PresentationSessionContext
{
    private VnPlaybackRuntimeState _playback;

    private bool _isStepCommandBusy;
    private bool _isBlockingInput;
    private bool _closeRequested;

    public bool IsStepCommandBusy => _isStepCommandBusy;
    public bool IsBlockingInput => _isBlockingInput;
    public bool CloseRequested => _closeRequested;

    public bool IsAutoMode => _playback != null && _playback.IsAutoMode;
    public bool IsSpeedUpMode => _playback != null && _playback.IsSpeedUpMode;
    public bool IsRapidSkipMode => _playback != null && _playback.IsRapidSkipMode;

    public float TimeScale => _playback != null ? _playback.TimeScale : 1f;
    public float AutoAdvanceDelay => _playback != null ? _playback.AutoAdvanceDelay : 1.5f;

    public PresentationSessionContext(VnPlaybackRuntimeState playback)
    {
        _playback = playback;
    }

    /// <summary>
    /// Must be called only by CommandRunScope.
    /// </summary>
    public void SetStepCommandBusy(bool busy)
    {
        _isStepCommandBusy = busy;
    }

    public void SetBlockingInput(bool blocking)
    {
        _isBlockingInput = blocking;
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
    }
}