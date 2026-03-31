using System;
using UnityEngine;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackRuntimeState _state;
    private readonly NodeRollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly IRollbackDialogueRestarter _restarter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly InlineEventMarkupHandler _inlineMarkupHandler;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly VnPlaybackSettings _playbackSettings;
    private readonly PresentationSessionBridge _presentationSessionBridge;

    private VnPlayMode _modeBeforeSeek;
    private bool _wasPauseIgnoredBeforeSeek;

    public bool IsSeeking => _state.IsSeeking;
    public bool CanRollback => !_state.IsSeeking && _history.CanRollbackOneStep();

    public RollbackController(
        RollbackRuntimeState state,
        NodeRollbackHistory history,
        YarnLineLifecycleBridge bridge,
        IRollbackDialogueRestarter restarter,
        DialogueAdvanceDispatcher dispatcher,
        InlineEventMarkupHandler inlineMarkupHandler,
        EllipsisBreathTypewriter typewriter,
        VnPlaybackSettings playbackSettings,
        PresentationSessionBridge presentationSessionBridge)
    {
        _state = state;
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _inlineMarkupHandler = inlineMarkupHandler;
        _typewriter = typewriter;
        _playbackSettings = playbackSettings;
        _presentationSessionBridge = presentationSessionBridge;

        _bridge.LineStart += OnLineStart;
        _bridge.LineFinishDisplaying += OnLineFinishDisplaying;
        _bridge.NodeCompleted += OnNodeCompleted;
    }

    public void RequestRollbackOneSrrtep()
    {
        if (!_history.TryGetPreviousPoint(out RollbackPoint target))
        { }
            
        BeginSeek(target);
    }

    public bool RequestRollbackOneStep()
    {
        if (!_history.TryGetPreviousPoint(out RollbackPoint target))
            return false;

        // if (target.presentationNodeIndex < 0 || target.presentationStepIndex < 0)
        //     return false;

        _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
        _inlineMarkupHandler.SetPauseIgnored(false);
        _inlineMarkupHandler.SetReplaySuppressed(
            suppressSignals: false,
            suppressMoves: false
        );
        _typewriter.SetSpeedMultiplier(1f);

        bool jumped = _presentationSessionBridge.JumpTo(
            target.presentationNodeIndex,
            target.presentationStepIndex
        );
        
        _typewriter.SetSpeedMultiplier(20f);

        _state.BeginRollback(target);
        _restarter.RestartNode(target.nodeName);

        if (!jumped)
            return false;

        _history.TrimAfterVisitedIndex(target.visitedIndex - 1);
        return true;
    }

    private void BeginSeek(RollbackPoint target)
    {
        _modeBeforeSeek = _playbackSettings.vnPlayMode;

        _state.BeginRollback(target);

        // rollback 시작 시엔 무조건 manual로 고정
        _playbackSettings.ChangePlayMode(VnPlayMode.Manual);

        // seek 중엔 pause / signal / move suppress
        _inlineMarkupHandler.SetPauseIgnored(true);
        _inlineMarkupHandler.SetReplaySuppressed(
            suppressSignals: true,
            suppressMoves: true
        );

        // seek는 아주 빠르게
        //UIManager.Instance.GetUI<DialogueUIRoot>().HideAllBoxes();
        _typewriter.SetSpeedMultiplier(20f);

        _restarter.RestartNode(target.nodeName);
    }
    
    
    private void EndSeekBeforeTargetLineDisplays()
    {
        int targetVisitedIndex = _state.TargetVisitedIndex;

        _state.EndRollback();

        _history.TrimAfterVisitedIndex(targetVisitedIndex - 1);

        _inlineMarkupHandler.SetPauseIgnored(false);
        _inlineMarkupHandler.SetReplaySuppressed(
            suppressSignals: false,
            suppressMoves: false
        );
        
        //UIManager.Instance.GetUI<DialogueUIRoot>().HideAllBoxes();
        _typewriter.SetSpeedMultiplier(1f);

        // rollback 후엔 안전하게 manual 유지
        _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
    }

    private void CancelSeek()
    {
        int targetVisitedIndex = _state.TargetVisitedIndex;
        _state.EndRollback();
        _history.TrimAfterVisitedIndex(targetVisitedIndex - 1);

        _inlineMarkupHandler.SetPauseIgnored(false);
        _inlineMarkupHandler.SetReplaySuppressed(
            suppressSignals: false,
            suppressMoves: false
        );
        
        //UIManager.Instance.GetUI<DialogueUIRoot>().ShowAllBoxes();
        _typewriter.SetSpeedMultiplier(1f);
        _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
    }

    private void OnLineStart(YarnLineMeta meta)
    {
        if (!_state.IsSeeking) return;

        if (_state.IsTarget(meta.nodeName, meta.lineId))
        {
            EndSeekBeforeTargetLineDisplays();
            return;
        }

        // HurryUp만 — Next는 OnLineFinishDisplaying에서
        _dispatcher.DispatchSeekHurryUp();
    }

    private void OnLineFinishDisplaying(YarnLineMeta meta)
    {
        if (!_state.IsSeeking) return;

        if (_state.IsTarget(meta.nodeName, meta.lineId))
            return;

        // 여기서만 Next
        _dispatcher.DispatchSeekNext();
    }

    private void OnNodeCompleted(string completedNodeName)
    {
        if (!_state.IsSeeking)
            return;

        // target을 못 찾고 node가 끝났으면 seek 실패 종료
        CancelSeek();
    }
    
    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.LineStart -= OnLineStart;
        _bridge.LineFinishDisplaying -= OnLineFinishDisplaying;
        _bridge.NodeCompleted -= OnNodeCompleted;
    }
}