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
    private bool _hasTarget;
    private bool _isSeeking;

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
        if (_isSeeking)
        {
            Debug.LogWarning("[VNLoadSeekDriver] PrepareForLoad ignored. Already seeking.");
            return;
        }

        _isSeeking = true;
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
            Debug.LogError("[VNLoadSeekDriver] BeginSeek failed. saveData has no nodeName.");
            Fail(onFail);
            return;
        }
        
        _lineAdvanceState?.MarkLoadSeek(saveData.nodeName, saveData.lineId);
        
        _target = saveData;
        _hasTarget = true;
        _onComplete = onComplete;
        _onFail = onFail;

        Subscribe();
        //_restarter.LoadGame(saveData.nodeName);
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
        if (!_isSeeking)
            return;

        if (!_hasTarget)
        {
            Complete();
            return;
        }

        if (IsTarget(meta))
        {
            Complete();
            return;
        }

        _dispatcher.DispatchSeekNext();
    }

    private bool IsTarget(YarnLineMeta meta)
    {
        if (!_hasTarget)
            return false;

        if (meta.nodeName != _target.nodeName)
            return false;

        // lineId가 비어 있으면 node 시작 지점으로 load
        if (string.IsNullOrWhiteSpace(_target.lineId))
            return true;

        if (meta.lineId != _target.lineId)
            return false;

        return true;
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

        _isSeeking = false;
        _hasTarget = false;
        _target = null;

        _onComplete = null;
        _onFail = null;
    }

    private void Subscribe()
    {
        if (_bridge == null)
            return;

        _bridge.LineEntered -= HandleLineEntered;
        _bridge.LineEntered += HandleLineEntered;
    }

    private void Unsubscribe()
    {
        if (_bridge == null)
            return;

        _bridge.LineEntered -= HandleLineEntered;
    }
    
    public void Dispose()
    {
        Unsubscribe();
    }
}