using System;

public sealed class AutoAdvanceScheduler
{
    private readonly YarnLineLifecycleBridge _yarnLineLifecycleBridge;
    
    private readonly Action _requestAdvance;
    private readonly VnPlaybackSettings _playbackSettings;
    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private readonly Func<double> _getNow;
    
    private double _nextAutoAdvanceAt = double.PositiveInfinity;

    public AutoAdvanceScheduler(
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        VnPlaybackSettings vnPlaybackSettings,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        Func<double> getNow)
    {
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;
        _playbackSettings = vnPlaybackSettings;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _getNow = getNow;

        RegisterToYarn();
    }
    
    private void RegisterToYarn()
    {
        _yarnLineLifecycleBridge.LineDisplayBegin -= NotifyLineStart;
        _yarnLineLifecycleBridge.LineDisplayBegin += NotifyLineStart;
        _yarnLineLifecycleBridge.LineFinishDisplaying -= NotifyLineFinishDisplaying;
        _yarnLineLifecycleBridge.LineFinishDisplaying += NotifyLineFinishDisplaying;
    }
    
    private void NotifyLineStart(YarnLineMeta meta) => _nextAutoAdvanceAt = double.PositiveInfinity;
    private void NotifyLineFinishDisplaying(YarnLineMeta meta) => _nextAutoAdvanceAt = _getNow() + _playbackSettings.autoModeDelaySeconds;
    
    
    public void Tick()
    {
        double t = _getNow();
        
        if (t >= _nextAutoAdvanceAt)
        {
            _nextAutoAdvanceAt = double.PositiveInfinity;
            _dialogueAdvanceDispatcher.DispatchAdvance();
        }
    }
    
    public void ResetAutoAdvanceTimer() => _nextAutoAdvanceAt = _getNow() + _playbackSettings.autoModeDelaySeconds;
    public void NotifyChoicesPresented() => _nextAutoAdvanceAt = double.PositiveInfinity;
    public void NotifyBacklogOpened() => _nextAutoAdvanceAt = double.PositiveInfinity;
    
    
    private void UnRegisterToYarn()
    {
        if (_yarnLineLifecycleBridge == null) return;

        _yarnLineLifecycleBridge.LineDisplayBegin -= NotifyLineStart;
        _yarnLineLifecycleBridge.LineFinishDisplaying -= NotifyLineFinishDisplaying;
    }
}