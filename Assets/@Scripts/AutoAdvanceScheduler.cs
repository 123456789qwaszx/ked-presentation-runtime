using System;
using UnityEngine;
using Yarn.Unity;

public sealed class AutoAdvanceScheduler
{
    private readonly YarnLineLifecycleBridge _yarnLineLifecycleBridge;
    
    private readonly Action _requestAdvance;
    private readonly VnUxState _uxState;
    private readonly VnFeaturePolicy _vnFeaturePolicy;
    private readonly DialogueAdvanceRouter _dialogueAdvanceRouter;
    private readonly Func<double> _getNow;
    
    private double _nextAutoAdvanceAt = double.PositiveInfinity;
    private bool _isAutoEnabled;
    private bool _lineFullyShown;

    public AutoAdvanceScheduler(
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        VnUxState uxState, 
        VnFeaturePolicy vnFeaturePolicy,
        DialogueAdvanceRouter dialogueAdvanceRouter,
        Func<double> getNow)
    {
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;
        _uxState = uxState;
        _vnFeaturePolicy = vnFeaturePolicy;
        _dialogueAdvanceRouter = dialogueAdvanceRouter;
        _getNow = getNow;

        RegisterHandler();
    }
    
    private void RegisterHandler()
    {
        _yarnLineLifecycleBridge.LineStart -= NotifyLineStart;
        _yarnLineLifecycleBridge.LineStart += NotifyLineStart;
        _yarnLineLifecycleBridge.LineFinishDisplaying -= NotifyLineFullyShown;
        _yarnLineLifecycleBridge.LineFinishDisplaying += NotifyLineFullyShown;
    }

    private void UnRegisterHandler()
    {
        if (_yarnLineLifecycleBridge == null) return;

        _yarnLineLifecycleBridge.LineStart -= NotifyLineStart;
        _yarnLineLifecycleBridge.LineFinishDisplaying -= NotifyLineFullyShown;
    }
    
    public void SetEnabled(bool enabled)
    {
        if (_isAutoEnabled == enabled)
            return;

        _isAutoEnabled = enabled;

        if (!_isAutoEnabled)
        {
            // Stop scheduling any auto-advance
            _nextAutoAdvanceAt = double.PositiveInfinity;
            return;
        }

        // Enabled: if the current line is already fully shown, schedule auto-advance now
        if (_lineFullyShown)
            _nextAutoAdvanceAt = _getNow() + _vnFeaturePolicy.autoDelaySeconds;
    }
    
    private void NotifyLineStart(YarnLineMeta meta)
    {
        _lineFullyShown = false;
        _nextAutoAdvanceAt = double.PositiveInfinity;
    }
    
    private void NotifyLineFullyShown(YarnLineMeta meta)
    {
        _lineFullyShown = true;

        if (!_isAutoEnabled)
            return;

        _nextAutoAdvanceAt = _getNow() + _vnFeaturePolicy.autoDelaySeconds;
    }
    
    public void NotifyChoicesPresented() => _nextAutoAdvanceAt = double.PositiveInfinity;
    public void NotifyBacklogOpened() => _nextAutoAdvanceAt = double.PositiveInfinity;
    
    public void Tick()
    {
        if (!_isAutoEnabled) return;
        if (!_lineFullyShown) return;

        if (_uxState.BacklogVisible) return;
        if (_uxState.ChoicesVisible) return;

        double t = _getNow();
        
        //Debug.Log($"{_getNow()}");
        if (t >= _nextAutoAdvanceAt)
        {
            //Debug.Log("{_getNow()}");
            _nextAutoAdvanceAt = double.PositiveInfinity;
            _lineFullyShown = false;
            _dialogueAdvanceRouter.DispatchAdvance();
        }
    }
}