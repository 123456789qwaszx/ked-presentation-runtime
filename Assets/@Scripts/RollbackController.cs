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
    private readonly YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;
    private readonly CommandExecutor _commandExecutor;

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
        PresentationSessionBridge presentationSessionBridge,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver,
        CommandExecutor commandExecutor
        )
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
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;
        _commandExecutor = commandExecutor;

        _bridge.LineStart += EndSeekBeforeTargetLineDisplays;
        _bridge.LineFinishDisplaying += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        if (!_history.TryGetPreviousPoint(out RollbackPoint target))
            return false;
    
        // if (target.presentationNodeIndex < 0 || target.presentationStepIndex < 0)
        //     return false;
        //
        // _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
        // _inlineMarkupHandler.SetPauseIgnored(true);
        // _inlineMarkupHandler.SetReplaySuppressed(
        //     suppressSignals: true,
        //     suppressMoves: true
        // );
        // _typewriter.SetSpeedMultiplier(1f);
    
        bool jumped = _presentationSessionBridge.JumpTo(
            target.presentationNodeIndex,
            target.presentationStepIndex
        );
    
        _history.TrimAfterVisitedIndex(target.visitedIndex);
        
        // _yarnBridgePlaybackDriver.ClearCollected();
        // _commandExecutor.Stop();
        _state.BeginRollback(target);
        _restarter.RestartNode(target.nodeName);
    
        if (!jumped)
            return false;
    
        return true;
    }
    
    private void EndSeekBeforeTargetLineDisplays(YarnLineMeta meta)
    {
        Debug.Log(_state.IsSeeking);
        if (!_state.IsSeeking) 
            return;

        Debug.Log($"@@@@@@@@@@@@@@@@{meta.rawText}");
        
        if (_state.IsTarget(meta.nodeName, meta.lineId))
        {
            Debug.Log("EndRollback!");
            _state.EndRollback();
            Debug.Log(_state.IsSeeking);
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

        _bridge.LinePrepared -= EndSeekBeforeTargetLineDisplays;
    }
}



// private void EndSeekBeforeTargetLineDisplays(YarnLineMeta meta)
// {
//     if (!_state.IsSeeking) 
//         return;
//
//     if (_state.IsTarget(meta.nodeName, meta.lineId))
//     {
//         int targetVisitedIndex = _state.TargetVisitedIndex;
//         _state.EndRollback();
//
//         _history.TrimAfterVisitedIndex(targetVisitedIndex - 1);
//
//         _inlineMarkupHandler.SetPauseIgnored(false);
//         _inlineMarkupHandler.SetReplaySuppressed(
//             suppressSignals: false,
//             suppressMoves: false
//         );
//         
//         _typewriter.SetSpeedMultiplier(1f);
//
//         _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
//         return;
//     }
//
//     // HurryUp만 — Next는 OnLineFinishDisplaying에서
//     _dispatcher.DispatchSeekHurryUp();
// }


// private void BeginSeek(RollbackPoint target)
// {
//     _modeBeforeSeek = _playbackSettings.vnPlayMode;
//
//     _state.BeginRollback(target);
//
//     // rollback 시작 시엔 무조건 manual로 고정
//     _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
//
//     // seek 중엔 pause / signal / move suppress
//     _inlineMarkupHandler.SetPauseIgnored(true);
//     _inlineMarkupHandler.SetReplaySuppressed(
//         suppressSignals: true,
//         suppressMoves: true
//     );
//
//     // seek는 아주 빠르게
//     //UIManager.Instance.GetUI<DialogueUIRoot>().HideAllBoxes();
//     _typewriter.SetSpeedMultiplier(20f);
//
//     _yarnBridgePlaybackDriver.ClearCollected();
//     _commandExecutor.Stop();
//     _restarter.RestartNode(target.nodeName);
//         
//     _history.TrimAfterVisitedIndex(target.visitedIndex - 1);
// }