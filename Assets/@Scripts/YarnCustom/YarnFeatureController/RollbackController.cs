using System;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly LinePresentationAdvanceState _lineAdvanceState;

    public RollbackController(
        RollbackHistory history,
        YarnLineLifecycleBridge bridge,
        DialogueAdvanceDispatcher dispatcher,
        LinePresentationAdvanceState lineAdvanceState)
    {
        _history = history;
        _bridge = bridge;
        _dispatcher = dispatcher;
        _lineAdvanceState = lineAdvanceState;

        _bridge.LineEntered -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered += EndSeekBeforeTargetLineDisplays;

        _bridge.LineEntered -= AddRollbackPoint;
        _bridge.LineEntered += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (_lineAdvanceState.RollbackTargetLinePending)
            return false;

        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
            return false;
        
        _lineAdvanceState.IsRollbackSeeking = true;
        
        _lineAdvanceState.RollbackPointBlocked = true;
        _lineAdvanceState.RollbackTargetLineId = target.lineId;
        _lineAdvanceState.RollbackTargetNodeName = target.nodeName;
        
        return true;
    }
    
    private void EndSeekBeforeTargetLineDisplays(YarnLineMeta meta)
    {
        if (!_lineAdvanceState.RollbackPointBlocked)
            return;

        if (!_lineAdvanceState.IsRollbackSeeking && !_lineAdvanceState.RollbackTargetLinePending)
        {
            _lineAdvanceState.RollbackPointBlocked = false;
            _lineAdvanceState.RollbackTargetLineId = null;
            _lineAdvanceState.RollbackTargetNodeName = null;
            
            return;
        }

        if (_lineAdvanceState.RollbackTargetLineId == meta.lineId || _lineAdvanceState.RollbackTargetNodeName == meta.nodeName)
        {
            _lineAdvanceState.IsRollbackSeeking = false;
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

// public bool RequestRollbackToHistoryIndex(int historyIndex)
// {
//     if (!_lineAdvanceState.IsRollbackSeeking && _lineAdvanceState.RollbackTargetLinePending)
//         return false;
//
//     if (!_history.TryPrepareRollbackToHistoryIndex(historyIndex, out RollbackPoint target))
//         return false;
//     return true;
// }