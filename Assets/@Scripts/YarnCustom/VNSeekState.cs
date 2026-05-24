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

    public void Begin(VNSeekKind kind, string nodeName, string lineId)
    {
        if (kind == VNSeekKind.None)
            return;

        Kind = kind;
        Phase = VNSeekPhase.Seeking;

        TargetNodeName = nodeName;
        TargetLineId = lineId;

        PendingNodeName = null;
        PendingLineId = null;

        Trace("Begin", $"kind={kind}, target={nodeName}/{lineId}");
    }

    public bool IsSeekingKind(VNSeekKind kind)
    {
        return Kind == kind && Phase == VNSeekPhase.Seeking;
    }

    public bool IsActiveKind(VNSeekKind kind)
    {
        return Kind == kind && Phase != VNSeekPhase.None;
    }

    public bool IsCurrentTarget(YarnLineMeta meta)
    {
        if (Phase != VNSeekPhase.Seeking)
            return false;

        if (!string.Equals(meta.nodeName, TargetNodeName, StringComparison.Ordinal))
            return false;

        // Empty lineId means "restore to the first line that enters this node".
        if (string.IsNullOrWhiteSpace(TargetLineId))
            return true;

        return string.Equals(meta.lineId, TargetLineId, StringComparison.Ordinal);
    }

    public bool MarkTargetReached(YarnLineMeta meta)
    {
        if (Phase != VNSeekPhase.Seeking)
        {
            Trace("MarkTargetReachedIgnored", $"meta={FormatMeta(meta)}, reason=PhaseNotSeeking");
            return false;
        }

        if (!IsCurrentTarget(meta))
        {
            Trace("MarkTargetReachedIgnored", $"meta={FormatMeta(meta)}, reason=NotTarget");
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

    public bool ConsumePendingTargetLine(string lineId)
    {
        if (Phase != VNSeekPhase.TargetLinePending)
        {
            Trace("ConsumePendingTargetLineIgnored", $"line={lineId}, reason=PhaseNotPending");
            return false;
        }

        if (string.IsNullOrWhiteSpace(PendingLineId))
        {
            Trace("ConsumePendingTargetLineIgnored", $"line={lineId}, reason=PendingLineEmpty");
            return false;
        }

        if (!string.Equals(PendingLineId, lineId, StringComparison.Ordinal))
        {
            Trace("ConsumePendingTargetLineIgnored", $"line={lineId}, pending={PendingLineId}, reason=LineMismatch");
            return false;
        }

        Trace("ConsumePendingTargetLine", $"line={lineId}");
        Clear("ConsumePendingTargetLine");
        return true;
    }

    public void Clear(string reason = "Clear")
    {
        if (Phase == VNSeekPhase.None && Kind == VNSeekKind.None)
        {
            Trace("ClearIgnored", $"reason={reason}, alreadyClear=True");
            return;
        }

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

        _trace.Trace("-----VNSeekState", evt, Snapshot(), note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}