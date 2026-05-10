
public sealed class LinePresentationAdvanceState
{
    private string _rollbackTargetLineId;
    private bool _isRollbackTargetLineReady;
    
    private bool _isRollbackSeeking;
    private bool _hasActiveLine;
    //private bool _isTransitioning;
    //private bool _isTypewriterRunning;
    private bool _isLineFullyShown = true;
    
    public bool IsRollbackSeeking => _isRollbackSeeking;

    public bool IsLineFullyShown => _hasActiveLine && _isLineFullyShown;

    public bool CanRequestNextLine => _hasActiveLine && _isLineFullyShown;

    public bool CanRequestHurryUp => _hasActiveLine && !_isLineFullyShown;
    
    // Rollback seek 자체는 끝났지만,
    // 다음 RunLineAsync에서 target line을 one-shot으로 처리해야 하는 상태.
    public bool HasRollbackTargetLineReady => _isRollbackTargetLineReady;

    // Controller 입장에서는 seek 중이거나 target line 처리 대기 중이면
    // 아직 rollback 흐름이 끝난 게 아니다.
    public bool IsRollbackActive =>
        _isRollbackSeeking ||
        _isRollbackTargetLineReady;
    
    public void MarkRollbackSeekStarted(string targetLineId)
    {
        _isRollbackSeeking = true;
        _rollbackTargetLineId = targetLineId;
        _isRollbackTargetLineReady = false;
    }

    public void MarkRollbackTargetLineReady()
    {
        if (string.IsNullOrWhiteSpace(_rollbackTargetLineId))
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
               !string.IsNullOrWhiteSpace(_rollbackTargetLineId) &&
               _rollbackTargetLineId == lineId;
    }

    public bool ConsumeRollbackTargetLine(string lineId)
    {
        if (!IsRollbackTargetLine(lineId))
            return false;

        _isRollbackTargetLineReady = false;
        _rollbackTargetLineId = null;
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
        _rollbackTargetLineId = null;
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