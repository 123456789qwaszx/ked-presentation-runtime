public sealed class RollbackController
{
    private readonly RollbackHistory _rollbackHistory;
    private readonly VNLinePresentationState _lineAdvanceState;

    public RollbackController(RollbackHistory rollbackHistory, VNLinePresentationState lineAdvanceState)
    {
        _rollbackHistory = rollbackHistory;
        _lineAdvanceState = lineAdvanceState;
    }

    public bool RequestRollbackOneStep()
    {
        if (_lineAdvanceState.IsSeekingActive)
            return false;
        
        if (!_rollbackHistory.GetRollbackPoint(out RollbackPoint target))
            return false;
        
        _rollbackHistory.ClearRollbackPoints();

        _lineAdvanceState.BeginRollbackSeek(target.nodeName, target.lineId);
        return true;
    }

    public void AddRollbackPoint(YarnLineMeta meta)
    {
        _rollbackHistory.AddRollbackPoint(meta);
    }
}