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
        ILinePresentationAborter linePresentationAborter)
    {
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _presentationSessionBridge = presentationSessionBridge;
        _presentationSessionContext = presentationSessionContext;
        _presentationUIRoot = presentationUIRoot;
        _linePresentationAborter = linePresentationAborter;

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
        //presentationUIRoot.
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

        // 1. 먼저 현재 Presenter 실행본을 구세대로 만든다.
        //    이전 RunLineAsync가 await 이후 깨어나도 visual commit/typewriter를 하지 못하게 한다.
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();

        // 2. 그 다음 rollback seek 모드로 진입한다.
        _presentationSessionContext.EnterRollbackSeek();
        RefreshDialogueUiSuppression();

        // 3. Yarn node를 다시 시작한다.
        //    DialogueRunner.Stop()은 rollback seek 중 호출하지 않는다.
        _restarter.RestartNode(target.nodeName);
    }

    private void EndRollbackSeek()
    {
        _hasTarget = false;
        _target = default;

        _presentationSessionContext.ExitRollbackSeek();
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