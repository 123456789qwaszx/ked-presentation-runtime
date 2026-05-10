
public sealed class LinePresentationAdvanceState
{
    public bool RollbackPointBlocked { get; set; }
    public string RollbackTargetLineId { get; set; }
    public string RollbackTargetNodeName { get; set; }
    public bool RollbackTargetLinePending { get; set; }

    public bool RollbackPointRecording => !RollbackPointBlocked;
    
    public void ResumeRollbackPointRecording()
    {
        RollbackPointBlocked = false;
        RollbackTargetLineId = null;
        RollbackTargetNodeName = null;
        RollbackTargetLinePending = false;
    }

    public bool IsRollback;


    
    
    private bool _isRollbackTargetLineReady;
    
    private bool _isRollbackSeeking;
    private bool _hasActiveLine;
    //private bool _isTransitioning;
    //private bool _isTypewriterRunning;
    private bool _isLineFullyShown = true;
    
    public bool IsRollbackSeeking => _isRollbackSeeking;

    public bool IsLineFullyShown => _hasActiveLine && _isLineFullyShown;

    // Controller 입장에서는 seek 중이거나 target line 처리 대기 중이면
    // 아직 rollback 흐름이 끝난 게 아니다.
    public bool IsRollbackActive =>
        !RollbackPointBlocked ||
        _isRollbackTargetLineReady;
    
    public void MarkRollbackSeekStarted(string targetLineId)
    {
        _isRollbackSeeking = true;
        RollbackTargetLineId = targetLineId;
        _isRollbackTargetLineReady = false;
    }

    public void MarkRollbackTargetLineReady()
    {
        if (string.IsNullOrWhiteSpace(RollbackTargetLineId))
        {
            ClearRollbackSeek();
            return;
        }

        _isRollbackSeeking = false;
        _isRollbackTargetLineReady = true;
    }

    public bool IsRollbackTargetLine(string lineId)
    {
        return _isRollbackTargetLineReady &&
               !string.IsNullOrWhiteSpace(RollbackTargetLineId) &&
               RollbackTargetLineId == lineId;
    }

    public bool ConsumeRollbackTargetLine(string lineId)
    {
        if (!IsRollbackTargetLine(lineId))
            return false;

        _isRollbackTargetLineReady = false;
        RollbackTargetLineId = null;
        return true;
    }

    public void MarkLineEntered()
    {
        _hasActiveLine = true;
        //_isTransitioning = true;
        //_isTypewriterRunning = false;
        _isLineFullyShown = false;
    }

    public void MarkTransitionFinished()
    {
        if (!_hasActiveLine)
            return;

        //_isTransitioning = false;
    }

    public void MarkTypewriterStarted()
    {
        if (!_hasActiveLine)
            return;

        //_isTypewriterRunning = true;
        _isLineFullyShown = false;
    }

    public void MarkLineDisplayCompleted()
    {
        if (!_hasActiveLine)
            return;

        //_isTransitioning = false;
        //_isTypewriterRunning = false;
        _isLineFullyShown = true;
    }

    public void MarkRollbackSeekLineEntered()
    {
        _isRollbackSeeking = true;
        _hasActiveLine = true;
        _isLineFullyShown = false;
    }

    public void ClearRollbackSeek()
    {
        _isRollbackSeeking = false;
        RollbackTargetLineId = null;
        _isRollbackTargetLineReady = false;
    }
    
    public void ClearActiveLine()
    {
        _hasActiveLine = false;
        //_isTransitioning = false;
        //_isTypewriterRunning = false;
        _isLineFullyShown = true;
    }

    public void Reset()
    {
        _hasActiveLine = false;
        //_isTransitioning = false;
        //_isTypewriterRunning = false;
        _isLineFullyShown = true;
    }
}