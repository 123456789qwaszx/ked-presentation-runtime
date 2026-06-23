using System;

public enum VNSeekKind
{
    None = 0,
    Rollback = 1,
    Load = 2,
}

public sealed class VNSeekState
{
    public VNSeekKind Kind { get; private set; } = VNSeekKind.None;
    public bool IsSeeking { get; private set; }

    public string TargetNodeName { get; private set; }
    public string TargetLineId { get; private set; }
    
    public void Begin(VNSeekKind kind, string nodeName, string lineId)
    {
        Kind = kind;
        IsSeeking = true;

        TargetNodeName = nodeName;
        TargetLineId = lineId;
    }

    public bool IsCurrentTarget(YarnLineMeta meta)
    {
        if (!IsSeeking)
            return false;

        if (!string.Equals(meta.nodeName, TargetNodeName, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(TargetLineId))
            return false;

        return string.Equals(meta.lineId, TargetLineId, StringComparison.Ordinal);
    }

    public void Clear()
    {
        Kind = VNSeekKind.None;
        IsSeeking = false;

        TargetNodeName = null;
        TargetLineId = null;
    }
}