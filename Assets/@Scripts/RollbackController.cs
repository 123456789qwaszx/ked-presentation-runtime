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

    public bool IsSeeking => _state.IsSeeking;
    public bool CanRollback => !_state.IsSeeking && _history.CanRollbackOneStep();

    public RollbackController(
        RollbackRuntimeState state,
        NodeRollbackHistory history,
        YarnLineLifecycleBridge bridge,
        IRollbackDialogueRestarter restarter,
        DialogueAdvanceDispatcher dispatcher,
        PresentationSessionBridge presentationSessionBridge)
    {
        _state = state;
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _presentationSessionBridge = presentationSessionBridge;

        _bridge.LineStart -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineStart += EndSeekBeforeTargetLineDisplays;

        _bridge.LineStart -= AddRollbackPoint;
        _bridge.LineStart += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (!_history.TryGetPreviousPoint(out RollbackPoint target))
            return false;

        bool jumped = _presentationSessionBridge.JumpTo(
            target.presentationNodeIndex,
            target.presentationStepIndex
        );

        if (!jumped)
            return false;

        // target 자신은 남기지 않고 잘라야
        // rollback 후 target line이 다시 완료될 때 1번만 정상 기록된다.
        _history.TrimAfterVisitedIndex(target.visitedIndex - 1);

        _state.BeginRollback(target);
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
            return;
        }

        _dispatcher.DispatchSeekNext();
    }

    private void AddRollbackPoint(YarnLineMeta meta)
    {
        _history.AddRollbackPoint(meta);
    }

    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.LineStart -= EndSeekBeforeTargetLineDisplays;
        _bridge.LineStart -= AddRollbackPoint;
    }
}