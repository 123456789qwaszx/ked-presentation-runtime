public sealed class LinePresentationAdvanceState
{
    private readonly VNSeekState _seek;
    private readonly VNLinePresentationState _line;
    private readonly VNTraceStream _trace;

    // seek.IsActive가 이미 커버하지만, 전환 기간 안전망으로 유지.
    // VNSeekState 단독으로 충분히 안정화되면 제거 예정.
    private bool _rollbackPointBlocked;

    public bool CanRecordRollbackPoint => !_rollbackPointBlocked && !_seek.IsActive;

    public string TargetNodeName => _seek.TargetNodeName;

    public bool IsSeeking => _seek.IsSeeking;
    public bool IsSeekingActive => _seek.IsActive;
    public bool IsLineFullyShown => _line.IsFullyShown;

    // Legacy compatibility aliases.
    public string RollbackTargetNodeName => TargetNodeName;
    public bool IsRollbackSeeking => IsSeeking;
    public bool IsRollbackActive => IsSeekingActive;

    public LinePresentationAdvanceState()
        : this(null)
    {
    }

    public LinePresentationAdvanceState(VNTraceStream trace)
    {
        _trace = trace;
        _seek = new VNSeekState(trace);
        _line = new VNLinePresentationState(trace);
    }

    public bool IsRollbackSeekTarget(YarnLineMeta meta)
    {
        bool result = _seek.IsTarget(meta);
        Trace("IsRollbackSeekTarget", $"meta={FormatMeta(meta)}, result={result}");
        return result;
    }

    public bool IsRollbackTargetLine(string lineId)
    {
        bool result = _seek.IsPendingTargetLine(lineId);
        Trace("IsRollbackTargetLine", $"line={lineId}, result={result}");
        return result;
    }

    public void BeginRollbackSeek(string nodeName, string lineId)
    {
        _rollbackPointBlocked = true;
        _line.MarkLineEntered();
        _seek.BeginRollbackSeek(nodeName, lineId);

        Trace("BeginRollbackSeek", $"target={nodeName}/{lineId}");
    }

    public void MarkLoadSeek(string nodeName, string lineId)
    {
        _rollbackPointBlocked = true;
        _line.MarkLineEntered();
        _seek.BeginLoadSeek(nodeName, lineId);

        Trace("MarkLoadSeek", $"target={nodeName}/{lineId}");
    }

    public void PrepareRollbackTargetLine(YarnLineMeta meta)
    {
        bool reached = _seek.MarkTargetReached(meta);
        Trace("PrepareRollbackTargetLine(meta)", $"meta={FormatMeta(meta)}, reached={reached}");
    }

    public void PrepareRollbackTargetLine()
    {
        if (string.IsNullOrWhiteSpace(_seek.TargetLineId))
        {
            Trace("PrepareRollbackTargetLineIgnored", "reason=no_meta_and_empty_target_line");

            UnityEngine.Debug.LogWarning(
                "[LinePresentationAdvanceState] PrepareRollbackTargetLine() was called without meta, " +
                "but TargetLineId is empty. Node-start seek must call PrepareRollbackTargetLine(YarnLineMeta).");

            return;
        }

        YarnLineMeta fallback = new YarnLineMeta(
            _seek.TargetNodeName,
            _seek.TargetLineId,
            rawText: "",
            charName: "");

        bool reached = _seek.MarkTargetReached(fallback);
        Trace("PrepareRollbackTargetLine()", $"fallback={FormatMeta(fallback)}, reached={reached}");
    }

    public bool ConsumeRollbackTargetLine(string lineId)
    {
        bool consumed = _seek.ConsumeTargetLine(lineId);

        if (consumed)
            _rollbackPointBlocked = false;

        Trace("ConsumeRollbackTargetLine(lineId)", $"line={lineId}, consumed={consumed}");

        return consumed;
    }

    public bool ConsumeRollbackTargetLine()
    {
        if (string.IsNullOrWhiteSpace(_seek.PendingLineId))
        {
            Trace("ConsumeRollbackTargetLineIgnored", "reason=pending_line_empty");

            UnityEngine.Debug.LogWarning(
                "[LinePresentationAdvanceState] ConsumeRollbackTargetLine() ignored. PendingLineId is empty.");

            return false;
        }

        return ConsumeRollbackTargetLine(_seek.PendingLineId);
    }

    public void ClearRollbackSeek()
    {
        _seek.Clear("ClearRollbackSeek");
        _rollbackPointBlocked = false;

        Trace("ClearRollbackSeek");
    }

    public void MarkLineEntered()
    {
        _line.MarkLineEntered();
        Trace("MarkLineEntered");
    }

    public void MarkLineDisplayCompleted()
    {
        _line.MarkLineDisplayCompleted();
        Trace("MarkLineDisplayCompleted");
    }

    public void Reset()
    {
        _seek.Clear("Reset");
        _rollbackPointBlocked = false;
        _line.Reset();

        Trace("Reset");
    }

    public string Snapshot()
    {
        return $"{_seek.Snapshot()}, {_line.Snapshot()}, rollbackPointBlocked={_rollbackPointBlocked}, canRecord={CanRecordRollbackPoint}";
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(LinePresentationAdvanceState), evt, Snapshot(), note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}