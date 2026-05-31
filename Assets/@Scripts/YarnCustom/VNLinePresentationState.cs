public sealed class VNLinePresentationState
{
    private readonly VNSeekState _seekState = new ();

    public VNSeekKind SeekKind => _seekState.Kind;

    public bool IsSeekingActive => _seekState.IsActive;

    public bool IsLineFullyShown { get; private set; } = true;

    public string SeekTargetNodeName => _seekState.TargetNodeName;
    
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
    }

    public bool IsSeekTargetLine(YarnLineMeta meta)
    {
        bool result = _seekState.IsCurrentTarget(meta);
        return result;
    }

    public bool MarkSeekTargetReached(YarnLineMeta meta)
    {
        bool reached = _seekState.MarkTargetReached(meta);
        return reached;
    }

    public bool IsPendingSeekTargetLine(string lineId)
    {
        bool result = _seekState.IsPendingTargetLine(lineId);
        return result;
    }

    public bool AcceptPendingSeekTargetLine(string lineId)
    {
        bool accepted = _seekState.ConsumePendingTargetLine(lineId);
        return accepted;
    }

    public void ClearSeek(string reason = "ClearSeek")
    {
        _seekState.Clear(reason);
    }
    
    public void MarkLineEntered()
    {
        IsLineFullyShown = true;
    }
    public void MarkLineDisplayCompleted(YarnLineMeta meta, string reason)
    {
        IsLineFullyShown = true;
    }
}