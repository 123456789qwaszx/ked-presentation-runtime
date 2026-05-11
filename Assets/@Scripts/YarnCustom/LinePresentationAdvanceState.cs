public sealed class LinePresentationAdvanceState
{
    private bool _rollbackPointBlocked;
    private string _rollbackTargetLineId;
    private string _rollbackTargetNodeName;
    private bool _rollbackTargetLinePending;

    private bool _isRollbackSeeking;
    private bool _isLineFullyShown = true;
    
    public bool CanRecordRollbackPoint => !_rollbackPointBlocked;
    public string RollbackTargetNodeName => _rollbackTargetNodeName;
    public bool IsRollbackSeeking => _isRollbackSeeking;
    public bool IsLineFullyShown => _isLineFullyShown;
    
    public bool IsRollbackActive => _isRollbackSeeking || _rollbackTargetLinePending;

    public void BeginRollbackSeek(string nodeName, string lineId)
    {
        _isRollbackSeeking = true;
        _rollbackPointBlocked = true;

        _rollbackTargetNodeName = nodeName;
        _rollbackTargetLineId = lineId;

        _rollbackTargetLinePending = false;

        _isLineFullyShown = false;
    }

    public void MarkLoadSeek(string nodeName, string lineId)
    {
        _isRollbackSeeking = true;
        
        _rollbackTargetLinePending = false;
        _isLineFullyShown = false;
        
        
        _rollbackTargetNodeName = nodeName;
        _rollbackTargetLineId = lineId;
    }

    public bool IsRollbackSeekTarget(YarnLineMeta meta)
    {
        return _rollbackTargetNodeName == meta.nodeName &&
               _rollbackTargetLineId == meta.lineId;
    }

    public bool IsRollbackTargetLine(string lineId)
    {
        return _rollbackTargetLinePending &&
               !string.IsNullOrWhiteSpace(_rollbackTargetLineId) &&
               _rollbackTargetLineId == lineId;
    }

    public void PrepareRollbackTargetLine()
    {
        if (string.IsNullOrWhiteSpace(_rollbackTargetLineId))
        {
            ClearRollbackSeek();
            return;
        }

        // seek종료, pending을 ConsumeRollbackTargetLine로 소비할 차례
        _isRollbackSeeking = false;
        _rollbackTargetLinePending = true;
    }

    public void ConsumeRollbackTargetLine()
    {
        //_isRollbackSeeking = false;
        _rollbackPointBlocked = false;

        _rollbackTargetLinePending = false;
        _rollbackTargetLineId = null;
        _rollbackTargetNodeName = null;
    }

    public void ClearRollbackSeek()
    {
        _isRollbackSeeking = false;
        _rollbackPointBlocked = false;

        _rollbackTargetLinePending = false;
        _rollbackTargetLineId = null;
        _rollbackTargetNodeName = null;
    }

    public void MarkLineEntered()
    {
        _isLineFullyShown = false;
    }

    public void MarkLineDisplayCompleted()
    {
        _isLineFullyShown = true;
    }

    public void Reset()
    {
        ClearRollbackSeek();
        _isLineFullyShown = true;
    }
}