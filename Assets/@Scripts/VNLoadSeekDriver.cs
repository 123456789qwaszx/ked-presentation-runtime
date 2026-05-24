using System;
using UnityEngine;

public sealed class VNLoadSeekDriver : IVNLoadSeekDriver, IDisposable
{
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly EpisodePlayer _restarter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly ILinePresentationAborter _linePresentationAborter;
    private readonly LinePresentationAdvanceState _lineAdvanceState;
    private readonly RollbackHistory _rollbackHistory;
    private readonly VNPlaytimeTracker _playtimeTracker;
    private readonly VNTraceStream _trace;

    private VNSaveData _target;

    private Action _onComplete;
    private Action _onFail;

    public VNLoadSeekDriver(
        YarnLineLifecycleBridge bridge,
        EpisodePlayer restarter,
        DialogueAdvanceDispatcher dispatcher,
        ILinePresentationAborter linePresentationAborter,
        LinePresentationAdvanceState lineAdvanceState,
        RollbackHistory rollbackHistory,
        VNPlaytimeTracker playtimeTracker,
        VNTraceStream trace = null)
    {
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _linePresentationAborter = linePresentationAborter;
        _lineAdvanceState = lineAdvanceState;
        _rollbackHistory = rollbackHistory;
        _playtimeTracker = playtimeTracker;
        _trace = trace;
    }

    public void PrepareForLoad()
    {
        Trace("PrepareForLoad");

        _rollbackHistory?.ClearRollbackHistory();
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();

        Trace("PrepareForLoadCompleted", "history cleared, current line aborted");
    }

    public void BeginSeek(VNSaveData saveData, Action onComplete, Action onFail)
    {
        if (saveData == null)
        {
            Debug.LogError("[VNLoadSeekDriver] BeginSeek failed. saveData is null.");
            Trace("BeginSeekFailed", "reason=SaveDataNull");
            Fail(onFail);
            return;
        }

        saveData.Normalize();

        if (!saveData.HasValidTarget())
        {
            Debug.LogError("[VNLoadSeekDriver] BeginSeek failed. saveData has no valid target.");
            Trace("BeginSeekFailed", $"reason=InvalidTarget, slot={saveData.slotId}");
            Fail(onFail);
            return;
        }

        _target = saveData;
        _onComplete = onComplete;
        _onFail = onFail;

        Trace("BeginSeekBeforeStartLoadSeek", $"target={saveData.nodeName}/{saveData.lineId}");

        _lineAdvanceState?.StartLoadSeek(saveData.nodeName, saveData.lineId);

        Trace("BeginSeekAfterStartLoadSeek", $"target={saveData.nodeName}/{saveData.lineId}");

        Subscribe();

        Trace("RestartDialogue", $"node={saveData.nodeName}");

        _restarter.StopDialogue();
        _restarter.StartGame(saveData.nodeName);
    }

    public void OnLoadComplete(VNSaveData saveData)
    {
        Trace("OnLoadComplete", saveData != null ? $"playtime={saveData.playtimeSeconds}" : "saveData=null");

        if (saveData != null && _playtimeTracker != null)
            _playtimeTracker.ResumeFromSave(saveData.playtimeSeconds);
    }

    private void HandleLineEntered(YarnLineMeta meta)
    {
        if (_lineAdvanceState != null && !_lineAdvanceState.IsLoadSeeking)
        {
            Trace(
                "LineEnteredIgnored",
                $"meta={FormatMeta(meta)}, reason=LineStateNotLoadSeeking, seekKind={_lineAdvanceState.SeekKind}, seekPhase={_lineAdvanceState.SeekPhase}");
            return;
        }

        Trace("LineEnteredDuringLoadSeek", $"meta={FormatMeta(meta)}");

        bool isTarget = IsTarget(meta);
        Trace("CheckTarget", $"meta={FormatMeta(meta)}, result={isTarget}");

        if (isTarget)
        {
            Trace("TargetReached", $"meta={FormatMeta(meta)}");

            // Do not clear line seek state in Complete(). The pending target must survive
            // until CustomLinePresenter consumes this exact line.
            _lineAdvanceState?.MarkSeekTargetReached(meta);

            Complete();
            return;
        }

        Trace("DispatchSeekNext", $"meta={FormatMeta(meta)}");
        _dispatcher.DispatchSeekNext();
    }

    private bool IsTarget(YarnLineMeta meta)
    {
        if (!string.Equals(meta.nodeName, _target.nodeName, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(_target.lineId))
            return true;

        return string.Equals(meta.lineId, _target.lineId, StringComparison.Ordinal);
    }

    private void Complete()
    {
        Trace("CompleteBeforeCleanup");

        Action callback = _onComplete;

        CleanupInternalState();

        Trace("CompleteAfterCleanup");

        callback?.Invoke();

        Trace("CompleteCallbackInvoked");
    }

    private void Fail(Action fallback = null)
    {
        Trace("FailBeforeCleanup");

        Action callback = fallback ?? _onFail;

        CleanupInternalState();

        Trace("FailAfterCleanup");

        callback?.Invoke();

        Trace("FailCallbackInvoked");
    }

    private void CleanupInternalState()
    {
        Trace("CleanupInternalStateBefore");

        Unsubscribe();

        _target = null;

        _onComplete = null;
        _onFail = null;

        // Intentionally do not call _lineAdvanceState.ClearSeek() here.
        // TargetLinePending belongs to CustomLinePresenter until ConsumeSeekTargetLine().
        Trace("CleanupInternalStateAfter");
    }

    private void Subscribe()
    {
        _bridge.LineEntered -= HandleLineEntered;
        _bridge.LineEntered += HandleLineEntered;

        Trace("Subscribed");
    }

    private void Unsubscribe()
    {
        _bridge.LineEntered -= HandleLineEntered;

        Trace("Unsubscribed");
    }

    public void Dispose()
    {
        Trace("Dispose");
        Unsubscribe();
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(VNLoadSeekDriver), evt, StateSnapshot(), note);
    }

    private string StateSnapshot()
    {
        string target = _target == null
            ? "target=null"
            : $"target={_target.nodeName}/{_target.lineId}";

        string lineState = _lineAdvanceState == null
            ? "lineState=null"
            : _lineAdvanceState.Snapshot();

        return $"{target}, lineState=[{lineState}]";
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}