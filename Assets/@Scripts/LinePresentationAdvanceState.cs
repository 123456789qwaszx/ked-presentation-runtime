public sealed class LinePresentationAdvanceState
{
    private readonly VNSeekState _seek = new();
    private readonly VNLinePresentationState _line = new();
    
    // seek.IsActive가 이미 커버하지만, 전환 기간 안전망으로 유지.
    // VNSeekState 단독으로 충분히 안정화되면 제거 예정.
    private bool _rollbackPointBlocked;
    
    // 기존 호출부 호환 프로퍼티(임시)
    public bool CanRecordRollbackPoint => !_rollbackPointBlocked && !_seek.IsActive;
    public string TargetNodeName => _seek.TargetNodeName;
    public bool IsSeeking => _seek.IsSeeking;
    public bool IsLineFullyShown => _line.IsFullyShown;
    public bool IsSeekingActive => _seek.IsActive;
    
    // seek target 판정
    public bool IsRollbackSeekTarget(YarnLineMeta meta) => _seek.IsTarget(meta);

    public bool IsRollbackTargetLine(string lineId) => _seek.IsPendingTargetLine(lineId);
    
    // seek 진입
    public void BeginRollbackSeek(string nodeName, string lineId)
    {
        _rollbackPointBlocked = true;
        _line.MarkLineEntered();
        _seek.BeginRollbackSeek(nodeName, lineId);
    }
    
    public void MarkLoadSeek(string nodeName, string lineId)
    {
        _rollbackPointBlocked = true;
        _line.MarkLineEntered();
        _seek.BeginLoadSeek(nodeName, lineId);
    }
    
    // meta를 쓰는 버전.
    // VNSeekState가 PendingLineId를 정확하게 기록해 consume 판정이 직관적임
    public void PrepareRollbackTargetLine(YarnLineMeta meta)
    {
        _seek.MarkTargetReached(meta);
    }

    // meta 없이 호출하는 기존 호출부 호환용.
    // 내부적으로 TargetLineId를 PendingLineId로 복사해 전이하므로,
    // node-start load(TargetLineId 빈 값)에서는 IsPendingTargetLine이
    // 항상 false를 반환해 consume이 되지 않을 수 있음.
    public void PrepareRollbackTargetLine()
    {
        if (string.IsNullOrWhiteSpace(_seek.TargetLineId))
        {
            UnityEngine.Debug.LogWarning(
                "[LinePresentationAdvanceState] PrepareRollbackTargetLine() was called without meta, " +
                "but TargetLineId is empty. Node-start seek must call PrepareRollbackTargetLine(YarnLineMeta).");

            return;
        }
        
        // VNSeekState에 no-arg MarkTargetReached가 없으므로
        // TargetLineId로 임시 YarnLineMeta를 만들어 위임.
        YarnLineMeta fallback = new YarnLineMeta(
            _seek.TargetNodeName,
            _seek.TargetLineId,
            rawText: "",
            charName: "");

        _seek.MarkTargetReached(fallback);
    }
    
    // target line 소비
    public bool ConsumeRollbackTargetLine(string lineId)
    {
        bool consumed = _seek.ConsumeTargetLine(lineId);

        if (consumed)
            _rollbackPointBlocked = false;

        return consumed;
    }
    // 기존 호환용. PendingLineId를 그대로 넘긴다.
    public bool ConsumeRollbackTargetLine()
    {
        if (string.IsNullOrWhiteSpace(_seek.PendingLineId))
        {
            UnityEngine.Debug.LogWarning(
                "[LinePresentationAdvanceState] ConsumeRollbackTargetLine() ignored. PendingLineId is empty.");

            return false;
        }

        return ConsumeRollbackTargetLine(_seek.PendingLineId);
    }

    // seek 강제 종료
    public void ClearRollbackSeek()
    {
        _seek.Clear();
        _rollbackPointBlocked = false;
    }
    
    // line 표시 상태
    public void MarkLineEntered()
    {
        _line.MarkLineEntered();
    }

    public void MarkLineDisplayCompleted()
    {
        _line.MarkLineDisplayCompleted();
    }

    // 세션 리셋
    public void Reset()
    {
        _seek.Clear();
        _rollbackPointBlocked = false;
        _line.Reset();
    }
    
    // 전체적으로 문제가 LocalizedLine을 토대로 Presenter가 자체적으로 판단해야하는 구조인데, 그러다보니 그걸 가지고 있는 Presenter가 직접 판단해서 상태를 스스로 갱신하는 식으로 사고가 흐름.
    // 그걸 외부에서 Presenter를 각자 판단해야 하는데, LocalizedLine은 의존도가 커지고, 상태가 외부 플러그인에 의해 정의되고, 불필요한게 많이 포함되어 있어서,
    // LineMeta라는 정보로 노출시켜서 관리하고자 함.
    
    
    
    #region legacy
    //private bool _rollbackPointBlocked;
    // private string _rollbackTargetLineId;
    // private string _rollbackTargetNodeName;
    // private bool _rollbackTargetLinePending;
    //
    // private bool _isRollbackSeeking;
    // private bool _isLineFullyShown = true;
    
    // public bool CanRecordRollbackPoint => !_rollbackPointBlocked;
    // public string RollbackTargetNodeName => _rollbackTargetNodeName;
    // public bool IsRollbackSeeking => _isRollbackSeeking;
    // public bool IsLineFullyShown => _isLineFullyShown;
    //
    // public bool IsRollbackActive => _isRollbackSeeking || _rollbackTargetLinePending;

    // public void BeginRollbackSeek(string nodeName, string lineId)
    // {
    //     _isRollbackSeeking = true;
    //     _rollbackPointBlocked = true;
    //
    //     _rollbackTargetNodeName = nodeName;
    //     _rollbackTargetLineId = lineId;
    //
    //     _rollbackTargetLinePending = false;
    //
    //     _isLineFullyShown = false;
    // }

    // public void MarkLoadSeek(string nodeName, string lineId)
    // {
    //     _isRollbackSeeking = true;
    //     _rollbackPointBlocked = true;
    //     
    //     _rollbackTargetLinePending = false;
    //     _isLineFullyShown = false;
    //     
    //     
    //     _rollbackTargetNodeName = nodeName;
    //     _rollbackTargetLineId = lineId;
    // }

    // public bool IsRollbackSeekTarget(YarnLineMeta meta)
    // {
    //     return _rollbackTargetNodeName == meta.nodeName &&
    //            _rollbackTargetLineId == meta.lineId;
    // }
    //
    // public bool IsRollbackTargetLine(string lineId)
    // {
    //     return _rollbackTargetLinePending &&
    //            !string.IsNullOrWhiteSpace(_rollbackTargetLineId) &&
    //            _rollbackTargetLineId == lineId;
    // }

    // public void PrepareRollbackTargetLine()
    // {
    //     if (string.IsNullOrWhiteSpace(_rollbackTargetLineId))
    //     {
    //         ClearRollbackSeek();
    //         return;
    //     }
    //
    //     // seek종료, pending을 ConsumeRollbackTargetLine로 소비할 차례
    //     _isRollbackSeeking = false;
    //     _rollbackTargetLinePending = true;
    // }

    // public void ConsumeRollbackTargetLine()
    // {
    //     //_isRollbackSeeking = false;
    //     _rollbackPointBlocked = false;
    //
    //     _rollbackTargetLinePending = false;
    //     _rollbackTargetLineId = null;
    //     _rollbackTargetNodeName = null;
    // }
    //
    // public void ClearRollbackSeek()
    // {
    //     _isRollbackSeeking = false;
    //     _rollbackPointBlocked = false;
    //
    //     _rollbackTargetLinePending = false;
    //     _rollbackTargetLineId = null;
    //     _rollbackTargetNodeName = null;
    // }

    // public void MarkLineEntered()
    // {
    //     _isLineFullyShown = false;
    // }
    //
    // public void MarkLineDisplayCompleted()
    // {
    //     _isLineFullyShown = true;
    // }
    //
    // public void Reset()
    // {
    //     ClearRollbackSeek();
    //     _isLineFullyShown = true;
    // }
    #endregion
}