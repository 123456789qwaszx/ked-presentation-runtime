public sealed class RollbackController
{
    private readonly RollbackHistory _history;
    private readonly LinePresentationAdvanceState _lineAdvanceState;

    public RollbackController(RollbackHistory history, LinePresentationAdvanceState lineAdvanceState)
    {
        _history = history;
        _lineAdvanceState = lineAdvanceState;
    }

    public bool RequestRollbackOneStep()
    {
        if (_lineAdvanceState.IsSeekActive)
            return false;
        
        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
            return false;

        _lineAdvanceState.StartRollbackSeek(target.nodeName, target.lineId);
        return true;
    }

    public bool RequestRollbackToHistoryIndex(int historyIndex)
    {
        if (_lineAdvanceState.IsSeekActive)
            return false;

        if (!_history.TryPrepareRollbackToHistoryIndex(historyIndex, out RollbackPoint target))
            return false;

        _lineAdvanceState.StartRollbackSeek(target.nodeName, target.lineId);
        return true;
    }

    public void AddRollbackPoint(YarnLineMeta meta)
    {
        if (!_lineAdvanceState.CanRecordRollbackPoint)
            return;

        _history.AddRollbackPoint(meta);
    }
}