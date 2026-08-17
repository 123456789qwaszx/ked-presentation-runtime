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
    
    // 라인이 안정된 경계까지 갔다(= Ready).
    public void MarkLineDisplayCompleted(YarnLineMeta meta, string reason)
    {
        IsLineFullyShown = true;
    }

    // 라인이 정상 경계 전에 무너짐.(= Released). 롤백 · 정지 · 선점으로 잘린 경우.
    public void MarkLineTornDown(YarnLineMeta meta, string reason)
    {
        IsLineFullyShown = false;
    }
}