using System;

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
        VnPlaybackSettings playbackSettings)
    {
        _state = state;
        _history = history;
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _inlineMarkupHandler = inlineMarkupHandler;
        _typewriter = typewriter;
        _playbackSettings = playbackSettings;

        _bridge.LineStart += OnLineStart;
        _bridge.LineFinishDisplaying += OnLineFinishDisplaying;
        _bridge.NodeCompleted += OnNodeCompleted;
    }

    public void RequestRollbackOneStep()
    {
        if (_state.IsSeeking)
            return;

        if (!_history.TryGetPreviousPoint(out RollbackPoint target))
            return;

        BeginSeek(target);
    }

    private void BeginSeek(in RollbackPoint target)
    {
        _modeBeforeSeek = _playbackSettings.vnPlayMode;

        _state.BeginSeek(target);

        // rollback 시작 시엔 무조건 manual로 고정
        _playbackSettings.ChangePlayMode(VnPlayMode.Manual);

        // seek 중엔 pause / signal / move suppress
        _inlineMarkupHandler.SetPauseIgnored(true);
        _inlineMarkupHandler.SetReplaySuppressed(
            suppressSignals: true,
            suppressMoves: true
        );

        // seek는 아주 빠르게
        _typewriter.SetSpeedMultiplier(30f);

        _restarter.RestartNode(target.nodeName);
    }

    private void OnLineStart(YarnLineMeta meta)
    {
        if (!_state.IsSeeking)
            return;

        if (_state.IsTarget(meta.nodeName, meta.lineId))
        {
            EndSeekBeforeTargetLineDisplays();
            return;
        }

        // target이 아닌 line은 즉시 hurry up
        _dispatcher.DispatchSeekAdvance();
    }

    private void OnLineFinishDisplaying(YarnLineMeta meta)
    {
        if (!_state.IsSeeking)
            return;

        if (_state.IsTarget(meta.nodeName, meta.lineId))
            return;

        // target이 아닌 line은 finish 후 즉시 next
        _dispatcher.DispatchSeekAdvance();
    }

    private void OnNodeCompleted(string completedNodeName)
    {
        if (!_state.IsSeeking)
            return;

        // target을 못 찾고 node가 끝났으면 seek 실패 종료
        CancelSeek();
    }

    private void EndSeekBeforeTargetLineDisplays()
    {
        _state.EndSeek();

        _inlineMarkupHandler.SetPauseIgnored(false);
        _inlineMarkupHandler.SetReplaySuppressed(
            suppressSignals: false,
            suppressMoves: false
        );

        _typewriter.SetSpeedMultiplier(1f);

        // rollback 후엔 안전하게 manual 유지
        _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
    }

    private void CancelSeek()
    {
        _state.EndSeek();

        _inlineMarkupHandler.SetPauseIgnored(false);
        _inlineMarkupHandler.SetReplaySuppressed(
            suppressSignals: false,
            suppressMoves: false
        );

        _typewriter.SetSpeedMultiplier(1f);
        _playbackSettings.ChangePlayMode(VnPlayMode.Manual);
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