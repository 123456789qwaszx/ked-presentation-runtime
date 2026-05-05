using System;
using UnityEngine;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackRuntimeState _state;
    private readonly NodeRollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly IRollbackDialogueRestarter _restarter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly PresentationSessionBridge _presentationSessionBridge;
    private readonly PresentationSessionContext _presentationSessionContext;
    private readonly PresentationUIRoot _presentationUIRoot;

    public RollbackController(
        RollbackRuntimeState state,
        NodeRollbackHistory history,
        YarnLineLifecycleBridge bridge,
        IRollbackDialogueRestarter restarter,
        DialogueAdvanceDispatcher dispatcher,
        PresentationSessionBridge presentationSessionBridge,
        PresentationSessionContext presentationSessionContext,
        PresentationUIRoot presentationUIRoot)
    {
        _state = state;
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _presentationSessionBridge = presentationSessionBridge;
        _presentationSessionContext = presentationSessionContext;
        _presentationUIRoot = presentationUIRoot;

        _bridge.LineEntered  -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered  += EndSeekBeforeTargetLineDisplays;

        _bridge.LineEntered  -= AddRollbackPoint;
        _bridge.LineEntered  += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (_state.IsSeeking)
            return false;

        if (!_history.TryGetPreviousPoint(out RollbackPoint target))
            return false;

        // bool jumped = _presentationSessionBridge.JumpTo(
        //     target.presentationNodeIndex,
        //     target.presentationStepIndex
        // );

        // target 자신은 남기지 않고 잘라야
        // rollback 후 target line이 다시 완료될 때 1번만 정상 기록된다.
        _history.TrimAfterVisitedIndex(target.visitedIndex - 1);

        _state.BeginRollback(target);

        _presentationSessionContext.EnterRollbackSeek();
        RefreshDialogueUiSuppression();

        _restarter.RestartNode(target.nodeName);

        return true;
    }

    private void EndSeekBeforeTargetLineDisplays(YarnLineMeta meta)
    {
        if (!_state.IsSeeking)
            return;

        if (_state.IsTarget(meta.nodeName, meta.lineId))
        {
            _state.EndRollback();

            _presentationSessionContext.ExitRollbackSeek();
            RefreshDialogueUiSuppression();

            return;
        }

        _dispatcher.DispatchSeekNext();
    }

    private void AddRollbackPoint(YarnLineMeta meta)
    {
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

        _bridge.LineEntered  -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineEntered  -= AddRollbackPoint;
    }
}