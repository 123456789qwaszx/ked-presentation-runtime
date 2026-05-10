using System;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly IRollbackDialogueRestarter _restarter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly ILinePresentationAborter _linePresentationAborter;
    private readonly LinePresentationAdvanceState _lineAdvanceState;

    public RollbackController(
        RollbackHistory history,
        YarnLineLifecycleBridge bridge,
        IRollbackDialogueRestarter restarter,
        DialogueAdvanceDispatcher dispatcher,
        ILinePresentationAborter linePresentationAborter,
        LinePresentationAdvanceState lineAdvanceState)
    {
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _linePresentationAborter = linePresentationAborter;
        _lineAdvanceState = lineAdvanceState;

        _bridge.LineEntered -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered += EndSeekBeforeTargetLineDisplays;

        _bridge.LineEntered -= AddRollbackPoint;
        _bridge.LineEntered += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (_lineAdvanceState.RollbackPointRecording || _lineAdvanceState.RollbackTargetLinePending)
            return false;

        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
            return false;

        _lineAdvanceState.RollbackTargetLineId = target.lineId;
        _lineAdvanceState.RollbackPointBlocked = true;
        
        BeginRollbackToTarget(target);
        return true;
    }

    public bool RequestRollbackToHistoryIndex(int historyIndex)
    {
        if (_lineAdvanceState.IsRollbackActive)
            return false;

        if (!_history.TryPrepareRollbackToHistoryIndex(historyIndex, out RollbackPoint target))
            return false;

        BeginRollbackToTarget(target);
        return true;
    }

    private void BeginRollbackToTarget(RollbackPoint target)
    {
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();
        _restarter.RestartNode(target.nodeName);
    }

    private void EndSeekBeforeTargetLineDisplays(YarnLineMeta meta)
    {
        if (_lineAdvanceState.RollbackPointRecording)
            return;

        if (!_lineAdvanceState.IsRollback)
        {
            _lineAdvanceState.ResumeRollbackPointRecording();
            return;
        }

        if (_lineAdvanceState.RollbackTargetLineId == meta.lineId)
        { // 다음 CustomLinePresenter.RunLineAsync가 이 line을 one-shot target으로 소비.
            _lineAdvanceState.RollbackTargetLinePending = true;
            return;
        }

        _dispatcher.DispatchSeekNext();
    }
    

    private void AddRollbackPoint(YarnLineMeta meta)
    {
        if (_lineAdvanceState.RollbackPointBlocked)
            return;

        _history.AddRollbackPoint(meta);
    }

    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.LineEntered -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered -= AddRollbackPoint;
    }
}