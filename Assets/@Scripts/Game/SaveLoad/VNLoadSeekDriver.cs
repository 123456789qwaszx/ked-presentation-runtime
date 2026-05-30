using System;

public sealed class VNLoadSeekDriver
{
    private readonly EpisodePlayer _restarter;
    private readonly VNLinePresentationState _lineAdvanceState;
    private readonly VNPlaytimeTracker _playtimeTracker;
    private readonly VNTraceStream _trace;

    private VNSaveData _target;

    private Action _onComplete;
    private Action _onFail;

    public bool IsActive
    {
        get { return _target != null; }
    }

    public VNSaveData Target
    {
        get { return _target; }
    }

    public VNLoadSeekDriver(
        EpisodePlayer restarter,
        VNLinePresentationState lineAdvanceState,
        VNPlaytimeTracker playtimeTracker,
        VNTraceStream trace = null)
    {
        _restarter = restarter;
        _lineAdvanceState = lineAdvanceState;
        _playtimeTracker = playtimeTracker;
        _trace = trace;
    }

    public void BeginSeek(VNSaveData saveData, Action onComplete, Action onFail)
    {
        if (saveData == null)
        {
            Fail(onFail);
            return;
        }

        _target = saveData;
        _onComplete = onComplete;
        _onFail = onFail;

        Trace("BeginSeek", $"target={saveData.nodeName}/{saveData.lineId}");

        _lineAdvanceState.BeginLoadSeek(saveData.nodeName, saveData.lineId);

        _restarter.RestartGame(saveData.nodeName);
    }

    public void Complete()
    {
        if (_target == null)
        {
            Trace("CompleteIgnored", "reason=NoTarget");
            return;
        }

        VNSaveData completedTarget = _target;
        Action callback = _onComplete;

        CleanupInternalState();

        Trace("Complete", $"target={completedTarget.nodeName}/{completedTarget.lineId}");

        callback?.Invoke();
    }

    public void Fail()
    {
        Fail(null);
    }

    private void Fail(Action fallback)
    {
        VNSaveData failedTarget = _target;
        Action callback = fallback ?? _onFail;

        CleanupInternalState();

        string targetText = failedTarget == null
            ? "target=null"
            : $"target={failedTarget.nodeName}/{failedTarget.lineId}";

        Trace("Fail", targetText);

        callback?.Invoke();
    }

    public void OnLoadComplete(VNSaveData saveData)
    {
        if (saveData != null && _playtimeTracker != null)
            _playtimeTracker.ResumeFromSave(saveData.playtimeSeconds);
    }

    private void CleanupInternalState()
    {
        _target = null;
        _onComplete = null;
        _onFail = null;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        string state = _lineAdvanceState == null
            ? "lineState=null"
            : _lineAdvanceState.Snapshot();

        _trace.Trace(nameof(VNLoadSeekDriver), evt, state, note);
    }
}