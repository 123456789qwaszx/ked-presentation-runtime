using System;

public enum VNSeekKind
{
    None = 0,
    Rollback = 1,
    Load = 2,
}

public enum VNSeekPhase
{
    None = 0,
    Seeking = 1,
    TargetLinePending = 2,
}

public sealed class VNSeekState
{
    private readonly VNTraceStream _trace;

    public VNSeekKind Kind { get; private set; } = VNSeekKind.None;
    public VNSeekPhase Phase { get; private set; } = VNSeekPhase.None;

    public string TargetNodeName { get; private set; }
    public string TargetLineId { get; private set; }

    public string PendingNodeName { get; private set; }
    public string PendingLineId { get; private set; }

    public bool IsActive => Phase != VNSeekPhase.None;
    public bool IsSeeking => Phase == VNSeekPhase.Seeking;
    public bool IsTargetLinePending => Phase == VNSeekPhase.TargetLinePending;

    public VNSeekState(VNTraceStream trace = null)
    {
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
        Kind = kind;
        Phase = VNSeekPhase.Seeking;

        TargetNodeName = nodeName;
        TargetLineId = lineId;

        PendingNodeName = null;
        PendingLineId = null;

        Trace("BeginSeek", $"kind={kind}, target={nodeName}/{lineId}");
    }

    public bool IsTarget(YarnLineMeta meta)
    {
        if (Phase != VNSeekPhase.Seeking)
            return false;

        if (!string.Equals(meta.nodeName, TargetNodeName, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(TargetLineId))
            return true;

        return string.Equals(meta.lineId, TargetLineId, StringComparison.Ordinal);
    }

    public bool MarkTargetReached(YarnLineMeta meta)
    {
        if (Phase != VNSeekPhase.Seeking)
        {
            Trace("MarkTargetReachedIgnored", $"meta={FormatMeta(meta)}, reason=phase_not_seeking");
            return false;
        }

        if (!IsTarget(meta))
        {
            Trace("MarkTargetReachedIgnored", $"meta={FormatMeta(meta)}, reason=not_target");
            return false;
        }

        Phase = VNSeekPhase.TargetLinePending;

        PendingNodeName = meta.nodeName;
        PendingLineId = meta.lineId;

        Trace("MarkTargetReached", $"meta={FormatMeta(meta)}");
        return true;
    }

    public bool IsPendingTargetLine(string lineId)
    {
        if (Phase != VNSeekPhase.TargetLinePending)
            return false;

        if (string.IsNullOrWhiteSpace(PendingLineId))
            return false;

        return string.Equals(PendingLineId, lineId, StringComparison.Ordinal);
    }

    public bool ConsumeTargetLine(string lineId)
    {
        if (Phase != VNSeekPhase.TargetLinePending)
        {
            Trace("ConsumeTargetLineIgnored", $"line={lineId}, reason=phase_not_pending");
            return false;
        }

        if (string.IsNullOrWhiteSpace(PendingLineId))
        {
            Trace("ConsumeTargetLineIgnored", $"line={lineId}, reason=pending_line_empty");
            return false;
        }

        if (!string.Equals(PendingLineId, lineId, StringComparison.Ordinal))
        {
            Trace("ConsumeTargetLineIgnored", $"line={lineId}, pending={PendingLineId}, reason=line_mismatch");
            return false;
        }

        Trace("ConsumeTargetLine", $"line={lineId}");
        Clear("ConsumeTargetLine");
        return true;
    }

    public void Clear(string reason = "Clear")
    {
        Trace("ClearRequested", $"reason={reason}");

        Kind = VNSeekKind.None;
        Phase = VNSeekPhase.None;

        TargetNodeName = null;
        TargetLineId = null;

        PendingNodeName = null;
        PendingLineId = null;

        Trace("Cleared");
    }

    public string Snapshot()
    {
        return $"seek={Kind}/{Phase}, target={TargetNodeName}/{TargetLineId}, pending={PendingNodeName}/{PendingLineId}, active={IsActive}";
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(VNSeekState), evt, Snapshot(), note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}