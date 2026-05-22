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

        _bridge.LineEntered -= HandleLineEnteredDuringRollbackSeek;
        _bridge.LineEntered += HandleLineEnteredDuringRollbackSeek;

        _bridge.LineEntered -= AddRollbackPoint;
        _bridge.LineEntered += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (_lineAdvanceState.IsRollbackActive)
            return false;
        
        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
            return false;
        
        _lineAdvanceState.BeginRollbackSeek(target.nodeName, target.lineId);

        return true;
    }

    public bool RequestRollbackToHistoryIndex(int historyIndex)
    {
        if (_lineAdvanceState.IsRollbackActive)
            return false;
        
        if (!_history.TryPrepareRollbackToHistoryIndex(historyIndex, out RollbackPoint target))
            return false;
        
        _lineAdvanceState.BeginRollbackSeek(
            target.nodeName,
            target.lineId);

        return true;
    }

    private void HandleLineEnteredDuringRollbackSeek(YarnLineMeta meta)
    {
        if (!_lineAdvanceState.IsRollbackSeeking)
            return;

        if (_lineAdvanceState.IsRollbackSeekTarget(meta))
        {
            _lineAdvanceState.PrepareRollbackTargetLine();
            return;
        }

        //Debug.Log($"[Rollback] Seek next. currentNode={meta.nodeName}, currentLine={meta.lineId}");

        _dispatcher.DispatchSeekNext();
    }

    private void AddRollbackPoint(YarnLineMeta meta)
    {
        if (!_lineAdvanceState.CanRecordRollbackPoint)
            return;

        _history.AddRollbackPoint(meta);
    }

    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.LineEntered -= HandleLineEnteredDuringRollbackSeek;
        _bridge.LineEntered -= AddRollbackPoint;
    }
}