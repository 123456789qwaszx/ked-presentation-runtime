using System;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly LinePresentationAdvanceState _lineAdvanceState;
    private readonly VNTraceStream _trace;

    public RollbackController(
        RollbackHistory history,
        YarnLineLifecycleBridge bridge,
        DialogueAdvanceDispatcher dispatcher,
        LinePresentationAdvanceState lineAdvanceState,
        VNTraceStream trace = null)
    {
        _history = history;
        _bridge = bridge;
        _dispatcher = dispatcher;
        _lineAdvanceState = lineAdvanceState;
        _trace = trace;

        _bridge.LineEntered -= HandleLineEnteredDuringRollbackSeek;
        _bridge.LineEntered += HandleLineEnteredDuringRollbackSeek;

        _bridge.LineEntered -= AddRollbackPoint;
        _bridge.LineEntered += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        Trace("RequestRollbackOneStep");

        if (_lineAdvanceState.IsSeekActive)
        {
            Trace("RequestRollbackOneStepRejected", "reason=SeekActive");
            return false;
        }

        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
        {
            Trace("RequestRollbackOneStepRejected", "reason=NoRollbackTarget");
            return false;
        }

        Trace("RequestRollbackOneStepAccepted", $"target={target.nodeName}/{target.lineId}, historyIndex={target.historyIndex}");

        _lineAdvanceState.StartRollbackSeek(target.nodeName, target.lineId);
        return true;
    }

    public bool RequestRollbackToHistoryIndex(int historyIndex)
    {
        Trace("RequestRollbackToHistoryIndex", $"historyIndex={historyIndex}");

        if (_lineAdvanceState.IsSeekActive)
        {
            Trace("RequestRollbackToHistoryIndexRejected", "reason=SeekActive");
            return false;
        }

        if (!_history.TryPrepareRollbackToHistoryIndex(historyIndex, out RollbackPoint target))
        {
            Trace("RequestRollbackToHistoryIndexRejected", $"historyIndex={historyIndex}");
            return false;
        }

        Trace("RequestRollbackToHistoryIndexAccepted", $"target={target.nodeName}/{target.lineId}, historyIndex={target.historyIndex}");

        _lineAdvanceState.StartRollbackSeek(target.nodeName, target.lineId);
        return true;
    }

    private void HandleLineEnteredDuringRollbackSeek(YarnLineMeta meta)
    {
        if (!_lineAdvanceState.IsRollbackSeeking)
            return;

        bool isTarget = _lineAdvanceState.IsSeekTarget(meta);
        Trace("CheckRollbackTarget", $"meta={FormatMeta(meta)}, result={isTarget}");

        if (isTarget)
        {
            Trace("RollbackTargetReached", $"meta={FormatMeta(meta)}");
            _lineAdvanceState.MarkSeekTargetReached(meta);
            return;
        }

        Trace("DispatchSeekNext", $"meta={FormatMeta(meta)}");
        _dispatcher.DispatchSeekNext();
    }

    private void AddRollbackPoint(YarnLineMeta meta)
    {
        if (!_lineAdvanceState.CanRecordRollbackPoint)
        {
            //Trace("RollbackPointSkipped", $"meta={FormatMeta(meta)}, reason=CanRecordRollbackPoint=false");
            return;
        }

        _history.AddRollbackPoint(meta);
        Trace("RollbackPointAdded", $"meta={FormatMeta(meta)}");
    }

    public void Dispose()
    {
        Trace("Dispose");

        if (_bridge == null)
            return;

        _bridge.LineEntered -= HandleLineEnteredDuringRollbackSeek;
        _bridge.LineEntered -= AddRollbackPoint;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        string state = _lineAdvanceState == null
            ? "lineState=null"
            : _lineAdvanceState.Snapshot();

        _trace.Trace(nameof(RollbackController), evt, state, note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}