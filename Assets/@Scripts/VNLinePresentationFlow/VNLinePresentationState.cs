public sealed class VNLinePresentationState : ISeekStateQuery
{
    private readonly VNSeekState _seekState = new ();
    public VNSeekKind SeekKind => _seekState.Kind;
    public bool IsSeekingActive => _seekState.IsSeeking;
    public string SeekTargetNodeName => _seekState.TargetNodeName;
    
    public bool IsLineFullyShown { get; private set; } = true;
    
    public void BeginRollbackSeek(string nodeName, string lineId) => BeginSeek(VNSeekKind.Rollback, nodeName, lineId);
    public void BeginLoadSeek(string nodeName, string lineId) => BeginSeek(VNSeekKind.Load, nodeName, lineId);
    
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

    public void ClearSeek()
    {
        _seekState.Clear();
    }
    
    public void MarkLineEntered()
    {
        IsLineFullyShown = false;
    }
    
    public void MarkLineDisplayCompleted(YarnLineMeta meta, string reason)
    {
        IsLineFullyShown = true;
    }
}