
public sealed class LinePresentationAdvanceState
{
    public bool RollbackPointBlocked { get; set; }
    public string RollbackTargetLineId { get; set; }
    public string RollbackTargetNodeName { get; set; }
    public bool RollbackTargetLinePending { get; set; }
    
    
    public bool IsRollbackSeeking { get; set; }
    
    
    public bool IsRollbackTargetLine(string lineId) => RollbackTargetLinePending && RollbackTargetLineId == lineId;
    
    public void ConsumeRollbackTargetLine()
    {
        RollbackTargetLinePending = false;
        RollbackTargetLineId = null;
        RollbackTargetNodeName = null;
    }
    
    public void ClearRollbackSeek()
    {
        // IsRollbackSeeking = false;
        // RollbackTargetLineId = null;
        // RollbackTargetNodeName = null;
        // RollbackTargetLinePending = false;
    }
    
    
    //private bool _isTypewriterRunning;
    private bool _isLineFullyShown = true;
    
    public bool IsLineFullyShown => _isLineFullyShown;
    
    public void MarkLineEntered()
    {
        //_isTypewriterRunning = false;
        _isLineFullyShown = false;
    }
    
    public void MarkTypewriterStarted()
    {
        //_isTypewriterRunning = true;
        _isLineFullyShown = false;
    }

    public void MarkLineDisplayCompleted()
    {
        _isLineFullyShown = true;
    }
    
    
    #region Load
    
    public void MarkLoadSeekLineEntered()
    {
        // IsRollbackSeeking = true;
        // _isLineFullyShown = false;
    }
    
    public void MarkLoadSeekStarted(string targetLineId)
    {
        // IsRollbackSeeking = true;
        // RollbackTargetLineId = targetLineId;
        // RollbackTargetLinePending = false;
    }

    public void MarkLoadTargetLineReady()
    {
        // if (string.IsNullOrWhiteSpace(RollbackTargetLineId))
        // {
        //     ClearRollbackSeek();
        //     return;
        // }

        // IsRollbackSeeking = false;
        // RollbackTargetLinePending = true;
    }
    
    public void ClearLoadSeek()
    {
        // IsRollbackSeeking = false;
        // RollbackTargetLineId = null;
        // RollbackTargetNodeName = null;
        // RollbackTargetLinePending = false;
    }
    #endregion
}