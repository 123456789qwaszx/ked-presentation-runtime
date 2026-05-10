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
    
    // Controller 입장에서는 seek 중이거나 target line 소비 대기 중이면 아직 rollback 흐름이 완전히 끝난 것이 아님
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
        // target line id가 없다면 pending 상태로 넘길 수 없다.
        // 안전하게 rollback seek 상태를 모두 정리.
        if (string.IsNullOrWhiteSpace(_rollbackTargetLineId))
        {
            ClearRollbackSeek();
            return;
        }

        // 여기서 seek는 끝난다. 하지만 아직 target line이 화면에 표시된 것은 아님.
        // 다음 CustomLinePresenter.RunLineAsync가 이 pending 상태를 보고 ConsumeRollbackTargetLine()을 호출.
        _isRollbackSeeking = false;
        _rollbackTargetLinePending = true;
    }

    public void ConsumeRollbackTargetLine()
    {
        // target line이 실제 Presenter에서 소비됨, 이 시점부터는 다시 RollbackPoint 기록을 허용.
        _isRollbackSeeking = false;
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

    public void MarkTypewriterStarted()
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