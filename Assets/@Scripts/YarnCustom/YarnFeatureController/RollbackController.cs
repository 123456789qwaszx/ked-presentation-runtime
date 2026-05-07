using System;
using UnityEngine;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly IRollbackDialogueRestarter _restarter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly PresentationSessionBridge _presentationSessionBridge;
    private readonly PresentationSessionContext _presentationSessionContext;
    private readonly PresentationUIRoot _presentationUIRoot;
    private readonly ILinePresentationAborter _linePresentationAborter;
    private readonly LinePresentationAdvanceState _lineAdvanceState;

    private RollbackPoint _target;
    private bool _hasTarget;

    public bool IsSeeking => _presentationSessionContext != null &&
                             _presentationSessionContext.IsRollbackSeeking;

    public RollbackController(
        RollbackHistory history,
        YarnLineLifecycleBridge bridge,
        IRollbackDialogueRestarter restarter,
        DialogueAdvanceDispatcher dispatcher,
        PresentationSessionBridge presentationSessionBridge,
        PresentationSessionContext presentationSessionContext,
        PresentationUIRoot presentationUIRoot,
        ILinePresentationAborter linePresentationAborter,
        LinePresentationAdvanceState lineAdvanceState)
    {
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _presentationSessionBridge = presentationSessionBridge;
        _presentationSessionContext = presentationSessionContext;
        _presentationUIRoot = presentationUIRoot;
        _linePresentationAborter = linePresentationAborter;
        _lineAdvanceState = lineAdvanceState;

        _bridge.LineEntered -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered += EndSeekBeforeTargetLineDisplays;

        _bridge.LineEntered -= AddRollbackPoint;
        _bridge.LineEntered += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (IsSeeking)
            return false;

        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
            return false;

        BeginRollbackToTarget(target);
        return true;
    }

    public bool RequestRollbackToHistoryIndex(int historyIndex)
    {
        if (IsSeeking)
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

        _presentationUIRoot.ShowDialogueBoxUI(false);
        
        // 1. 현재 LinePresenter 실행본을 먼저 무효화한다.
        //    이전 RunLineAsync가 await 이후 깨어나도 visual commit/typewriter를 하지 못하게 한다.
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();

        // 2. AdvanceGate가 typewriter.IsComplete 같은 낡은 상태를 보고
        //    "현재 라인이 이미 다 표시됐다"고 오판하지 못하게 막는다.
        _lineAdvanceState?.EnterRollbackSeek();

        // 3. Presentation 전체를 rollback seek 모드로 전환한다.
        //    이 상태에서는 FadeIn/FadeOut/Typewriter 같은 관객용 연출이 생략되어야 한다.
        _presentationSessionContext.EnterRollbackSeek();

        // 4. Dialogue UI suppression을 갱신한다.
        RefreshDialogueUiSuppression();

        // 5. Yarn node를 다시 시작한다.
        //    DialogueRunner.Stop()은 rollback seek 중 호출하지 않는다.
        _restarter.RestartNode(target.nodeName);
    }

    private void EndRollbackSeek()
    {
        _hasTarget = false;
        _target = default;
        
        //_presentationUIRoot.ShowDialogueBoxUI(true);

        // 1. Presentation rollback seek 종료.
        _presentationSessionContext.ExitRollbackSeek();

        // 2. AdvanceGate rollback lock 해제.
        //    단, 여기서 "line fully shown"으로 만들지는 않는다.
        //    target line의 실제 표시 시작/완료는 CustomLinePresenter가 다시 소유한다.
        _lineAdvanceState?.ExitRollbackSeek();

        // 3. Dialogue UI suppression 해제.
        RefreshDialogueUiSuppression();
    }

    private void EndSeekBeforeTargetLineDisplays(YarnLineMeta meta)
    {
        if (!IsSeeking)
            return;

        if (!_hasTarget)
        {
            EndRollbackSeek();
            return;
        }

        if (IsTarget(meta))
        {
            EndRollbackSeek();
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
        if (IsSeeking)
            return;

        _history.AddRollbackPoint(meta);
    }

    private void RefreshDialogueUiSuppression()
    {
        if (_presentationUIRoot == null)
            return;

        _presentationUIRoot.RefreshDialogueUiSuppression(_presentationSessionContext);
    }

    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.LineEntered -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered -= AddRollbackPoint;
    }
}