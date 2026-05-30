public sealed class VNLinePresentationState
{
    private readonly VNSeekState _seekState;
    private readonly VNTraceStream _trace;

    public VNSeekKind SeekKind => _seekState.Kind;
    public VNSeekPhase SeekPhase => _seekState.Phase;

    public bool IsSeekingActive => _seekState.IsActive;
    public bool IsSeekPassingThrough => _seekState.IsSeeking;

    public bool IsLineFullyShown { get; private set; } = true;

    public bool CanRecordRollbackPoint => !_seekState.IsActive;

    public string SeekTargetNodeName => _seekState.TargetNodeName;
    public string SeekTargetLineId => _seekState.TargetLineId;

    public VNLinePresentationState(VNTraceStream trace = null)
    {
        _seekState = new VNSeekState(trace);
        _trace = trace;
    }

    public void BeginRollbackSeek(string nodeName, string lineId)
    {
        BeginSeek(VNSeekKind.Rollback, nodeName, lineId);
    }

    public void BeginLoadSeek(string nodeName, string lineId)
    {
        BeginSeek(VNSeekKind.Load, nodeName, lineId);
    }

    private void BeginSeek(VNSeekKind kind, string nodeName, string lineId)
    {
        IsLineFullyShown = false;
        _seekState.Begin(kind, nodeName, lineId);

        Trace("BeginSeek", $"kind={kind}, target={nodeName}/{lineId}");
    }

    public bool IsSeekTargetLine(YarnLineMeta meta)
    {
        bool result = _seekState.IsCurrentTarget(meta);
        Trace("IsSeekTargetLine", $"meta={FormatMeta(meta)}, result={result}");
        return result;
    }

    public bool MarkSeekTargetReached(YarnLineMeta meta)
    {
        bool reached = _seekState.MarkTargetReached(meta);
        Trace("MarkSeekTargetReached", $"meta={FormatMeta(meta)}, reached={reached}");
        return reached;
    }

    public bool IsPendingSeekTargetLine(string lineId)
    {
        bool result = _seekState.IsPendingTargetLine(lineId);
        Trace("IsPendingSeekTargetLine", $"line={lineId}, result={result}");
        return result;
    }

    public bool AcceptPendingSeekTargetLine(string lineId)
    {
        bool accepted = _seekState.ConsumePendingTargetLine(lineId);
        Trace("AcceptPendingSeekTargetLine", $"line={lineId}, accepted={accepted}");
        return accepted;
    }

    public void ClearSeek(string reason = "ClearSeek")
    {
        _seekState.Clear(reason);
        Trace("ClearSeek", $"reason={reason}");
    }

    public void MarkLineEntered(YarnLineMeta meta)
    {
        IsLineFullyShown = false;
        Trace("MarkLineEntered", $"meta={FormatMeta(meta)}");
    }

    public void MarkLineDisplayCompleted(YarnLineMeta meta)
    {
        IsLineFullyShown = true;
        Trace("MarkLineDisplayCompleted", $"meta={FormatMeta(meta)}");
    }

    public void Reset()
    {
        _seekState.Clear("Reset");
        IsLineFullyShown = true;

        Trace("Reset");
    }

    public string Snapshot()
    {
        return $"{_seekState.Snapshot()}, lineFullyShown={IsLineFullyShown}, canRecordRollbackPoint={CanRecordRollbackPoint}";
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace("@VNLinePresentationState", evt, Snapshot(), note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}