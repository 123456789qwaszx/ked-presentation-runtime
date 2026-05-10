using System;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly IRollbackDialogueRestarter _restarter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly ILinePresentationAborter _linePresentationAborter;
    private readonly LinePresentationAdvanceState _lineAdvanceState;

    private RollbackPoint _target;
    private bool _hasTarget;
    

    public RollbackController(
        RollbackHistory history,
        YarnLineLifecycleBridge bridge,
        IRollbackDialogueRestarter restarter,
        DialogueAdvanceDispatcher dispatcher,
        ILinePresentationAborter linePresentationAborter,
        LinePresentationAdvanceState lineAdvanceState)
    {
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _linePresentationAborter = linePresentationAborter;
        _lineAdvanceState = lineAdvanceState;

        _bridge.LineEntered -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered += EndSeekBeforeTargetLineDisplays;

        _bridge.LineEntered -= AddRollbackPoint;
        _bridge.LineEntered += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (_lineAdvanceState.IsRollbackActive)
            return false;

        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
            return false;

        BeginRollbackToTarget(target);
        return true;
    }

    public bool RequestRollbackToHistoryIndex(int historyIndex)
    {
        if (_lineAdvanceState.IsRollbackActive)
            return false;

        if (!_history.TryPrepareRollbackToHistoryIndex(historyIndex, out RollbackPoint target))
            return false;

        BeginRollbackToTarget(target);
        return true;
    }

    private void BeginRollbackToTarget(RollbackPoint target)
    {
        _target = target;
        _hasTarget = true;

        // Rollback 상태를 먼저 세운다.
        // 이후 abort/close 과정에서 다른 시스템이 context를 읽어도 이미 seek 상태여야 한다.
        _lineAdvanceState.MarkRollbackSeekStarted(target.lineId);

        // AdvanceGate가 기존 typewriter 상태를 보고 현재 line을 완료로 오판하지 않게 잠근다.
        _lineAdvanceState?.MarkRollbackSeekLineEntered();

        // 현재 Presenter 실행본과 현재 DialogueBox를 실제로 닫는다.
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();

        _restarter.RestartNode(target.nodeName);
    }

    private void EndRollbackSeekAtTarget()
    {
        _hasTarget = false;
        _target = default;

        // 아직 target line을 표시한 것은 아니다.
        // 다음 CustomLinePresenter.RunLineAsync가 이 line을 one-shot target으로 소비한다.
        _lineAdvanceState.MarkRollbackTargetLineReady();
    }

    private void EndRollbackSeek()
    {
        _hasTarget = false;
        _target = default;

        _lineAdvanceState.ClearRollbackSeek();
    }

    private void EndSeekBeforeTargetLineDisplays(YarnLineMeta meta)
    {
        if (!_lineAdvanceState.IsRollbackSeeking)
            return;

        if (!_hasTarget)
        {
            EndRollbackSeek();
            return;
        }

        if (IsTarget(meta))
        {
            EndRollbackSeekAtTarget();
            return;
        }

        _dispatcher.DispatchSeekNext();
    }

    private bool IsTarget(YarnLineMeta meta)
    {
        if (!_hasTarget)
            return false;

        return _target.nodeName == meta.nodeName &&
               _target.lineId == meta.lineId;
    }

    private void AddRollbackPoint(YarnLineMeta meta)
    {
        if (_lineAdvanceState.IsRollbackSeeking)
            return;

        _history.AddRollbackPoint(meta);
    }

    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.LineEntered -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered -= AddRollbackPoint;
    }
}