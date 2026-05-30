public sealed class LinePresentationAdvanceState
{
    private readonly VNSeekState _seek;
    private readonly VNLinePresentationState _line;
    private readonly VNTraceStream _trace;

    private bool _rollbackPointBlocked;

    public VNSeekKind SeekKind => _seek.Kind;
    public VNSeekPhase SeekPhase => _seek.Phase;

    public bool IsSeekActive => _seek.IsActive;
    public bool IsSeeking => _seek.IsSeeking;
    public bool IsTargetLinePending => _seek.IsTargetLinePending;

    public bool IsRollbackSeeking => _seek.IsSeekingKind(VNSeekKind.Rollback);
    public bool IsRollbackSeekActive => _seek.IsActiveKind(VNSeekKind.Rollback);

    public bool IsLoadSeeking => _seek.IsSeekingKind(VNSeekKind.Load);
    public bool IsLoadSeekActive => _seek.IsActiveKind(VNSeekKind.Load);

    public bool IsLineFullyShown => _line.IsFullyShown;

    // Kept because rollback history is a separate concept from seek ownership.
    public bool CanRecordRollbackPoint => !_rollbackPointBlocked && !_seek.IsActive;

    // These aliases keep the rest of the current code compiling while the call sites migrate.
    public bool IsSeekingActive => IsSeekActive;
    public bool IsRollbackActive => IsRollbackSeekActive;
    public string TargetNodeName => _seek.TargetNodeName;

    public LinePresentationAdvanceState(VNTraceStream trace)
    {
        _trace = trace;
        _seek = new VNSeekState(trace);
        _line = new VNLinePresentationState();
    }

    public void StartRollbackSeek(string nodeName, string lineId)
    {
        StartSeek(VNSeekKind.Rollback, nodeName, lineId);
    }

    public void StartLoadSeek(string nodeName, string lineId)
    {
        StartSeek(VNSeekKind.Load, nodeName, lineId);
    }

    private void StartSeek(VNSeekKind kind, string nodeName, string lineId)
    {
        _rollbackPointBlocked = true;
        _line.MarkLineEntered();
        _seek.Begin(kind, nodeName, lineId);

        Trace("StartSeek", $"kind={kind}, target={nodeName}/{lineId}");
    }

    public bool IsSeekTarget(YarnLineMeta meta)
    {
        bool result = _seek.IsCurrentTarget(meta);
        Trace("IsSeekTarget", $"meta={FormatMeta(meta)}, result={result}");
        return result;
    }

    public bool IsPendingSeekTargetLine(string lineId)
    {
        bool result = _seek.IsPendingTargetLine(lineId);
        Trace("IsPendingSeekTargetLine", $"line={lineId}, result={result}");
        return result;
    }

    public bool MarkSeekTargetReached(YarnLineMeta meta)
    {
        bool reached = _seek.MarkTargetReached(meta);
        Trace("MarkSeekTargetReached", $"meta={FormatMeta(meta)}, reached={reached}");
        return reached;
    }

    public bool ConsumeSeekTargetLine(string lineId)
    {
        bool consumed = _seek.ConsumePendingTargetLine(lineId);

        if (consumed)
            _rollbackPointBlocked = false;

        Trace("ConsumeSeekTargetLine", $"line={lineId}, consumed={consumed}");
        return consumed;
    }

    public void ClearSeek(string reason = "ClearSeek")
    {
        _seek.Clear(reason);
        _rollbackPointBlocked = false;

        Trace("ClearSeek", $"reason={reason}");
    }

    public void MarkLineEntered()
    {
        _line.MarkLineEntered();
        Trace("MarkLineEntered");
    }

    public void MarkLineDisplayCompleted()
    {
        _line.MarkLineDisplayCompleted();
        Trace("-=-=-=-MarkLineDisplayCompleted-=-=-=-");
    }

    public void Reset()
    {
        ClearSeek("Reset");
        _line.Reset();

        Trace("Reset");
    }

    public string Snapshot()
    {
        return $"{_seek.Snapshot()}, rollbackPointBlocked={_rollbackPointBlocked}, canRecord={CanRecordRollbackPoint}";
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace("@LinePresentationAdvanceState", evt, Snapshot(), note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}