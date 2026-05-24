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
    public VNSeekKind Kind { get; private set; } = VNSeekKind.None;
    public VNSeekPhase Phase { get; private set; } = VNSeekPhase.None;

    public string TargetNodeName { get; private set; }
    public string TargetLineId { get; private set; }

    public string PendingNodeName { get; private set; }
    public string PendingLineId { get; private set; }

    public bool IsActive => Phase != VNSeekPhase.None;
    public bool IsSeeking => Phase == VNSeekPhase.Seeking;
    public bool IsTargetLinePending => Phase == VNSeekPhase.TargetLinePending;

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
    }

    public bool IsTarget(YarnLineMeta meta)
    {
        if (Phase != VNSeekPhase.Seeking)
            return false;

        if (meta.nodeName != TargetNodeName)
            return false;

        if (string.IsNullOrWhiteSpace(TargetLineId))
            return true;

        return meta.lineId == TargetLineId;
    }

    public void MarkTargetReached(YarnLineMeta meta)
    {
        if (Phase != VNSeekPhase.Seeking)
            return;

        Phase = VNSeekPhase.TargetLinePending;

        PendingNodeName = meta.nodeName;
        PendingLineId = meta.lineId;
    }

    public bool IsPendingTargetLine(string lineId)
    {
        if (Phase != VNSeekPhase.TargetLinePending)
            return false;

        if (string.IsNullOrWhiteSpace(PendingLineId))
            return false;

        return PendingLineId == lineId;
    }

    public void ConsumeTargetLine(string lineId)
    {
        if (Phase != VNSeekPhase.TargetLinePending)
            return;

        if (!string.IsNullOrWhiteSpace(PendingLineId) && PendingLineId != lineId)
            return;

        Clear();
    }

    public void Clear()
    {
        Kind = VNSeekKind.None;
        Phase = VNSeekPhase.None;

        TargetNodeName = null;
        TargetLineId = null;

        PendingNodeName = null;
        PendingLineId = null;
    }
}