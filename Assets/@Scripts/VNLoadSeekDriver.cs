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
        VNPlaytimeTracker playtimeTracker)
    {
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _linePresentationAborter = linePresentationAborter;
        _lineAdvanceState = lineAdvanceState;
        _rollbackHistory = rollbackHistory;
        _playtimeTracker = playtimeTracker;
    }

    public void PrepareForLoad()
    {
        _rollbackHistory?.ClearRollbackHistory();
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();
    }

    public void BeginSeek(VNSaveData saveData, Action onComplete, Action onFail)
    {
        if (saveData == null)
        {
            Debug.LogError("[VNLoadSeekDriver] BeginSeek failed. saveData is null.");
            Fail(onFail);
            return;
        }

        saveData.Normalize();

        if (!saveData.HasValidTarget())
        {
            Debug.LogError("[VNLoadSeekDriver] BeginSeek failed. saveData has no valid target.");
            Fail(onFail);
            return;
        }

        _target = saveData;
        _onComplete = onComplete;
        _onFail = onFail;

        _lineAdvanceState.StartLoadSeek(saveData.nodeName, saveData.lineId);

        Subscribe();

        _restarter.StopDialogue();
        _restarter.StartGame(saveData.nodeName);
    }

    public void OnLoadComplete(VNSaveData saveData)
    {
        if (saveData != null && _playtimeTracker != null)
            _playtimeTracker.ResumeFromSave(saveData.playtimeSeconds);
    }

    private void HandleLineEntered(YarnLineMeta meta)
    {
        if (_lineAdvanceState != null && !_lineAdvanceState.IsLoadSeeking)
            return;

        bool isTarget = IsTarget(meta);
        if (isTarget)
        {
            _lineAdvanceState?.MarkSeekTargetReached(meta);

            Complete();
            return;
        }
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
        Action callback = _onComplete;
        CleanupInternalState();
        
        callback?.Invoke();
    }

    private void Fail(Action fallback = null)
    {
        Action callback = fallback ?? _onFail;
        CleanupInternalState();
        
        callback?.Invoke();
    }

    private void CleanupInternalState()
    {
        Unsubscribe();

        _target = null;
        _onComplete = null;
        _onFail = null;
    }

    private void Subscribe()
    {
        _bridge.LineEntered -= HandleLineEntered;
        _bridge.LineEntered += HandleLineEntered;
    }

    private void Unsubscribe()
    {
        _bridge.LineEntered -= HandleLineEntered;
    }

    public void Dispose()
    {
        Unsubscribe();
    }
}