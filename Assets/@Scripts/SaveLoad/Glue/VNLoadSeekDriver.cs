using System;
using UnityEngine;

public interface IVNLoadDialogueRestarter
{
    void RestartNodeForLoad(string nodeName);
}

public sealed class VNLoadSeekDriver : IVNLoadSeekDriver, IDisposable
{
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly IVNLoadDialogueRestarter _restarter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly ILinePresentationAborter _linePresentationAborter;
    private readonly LinePresentationAdvanceState _lineAdvanceState;
    private readonly RollbackHistory _rollbackHistory;
    private readonly VNSaveRuntimeState _saveRuntimeState;
    private readonly VNPlaytimeTracker _playtimeTracker;

    private VNSaveData _target;
    private bool _hasTarget;
    private bool _isSeeking;

    private Action _onComplete;
    private Action _onFail;

    private int _matchedLineCountInNode;

    public bool IsSeeking => _isSeeking;

    public VNLoadSeekDriver(
        YarnLineLifecycleBridge bridge,
        IVNLoadDialogueRestarter restarter,
        DialogueAdvanceDispatcher dispatcher,
        ILinePresentationAborter linePresentationAborter,
        LinePresentationAdvanceState lineAdvanceState,
        RollbackHistory rollbackHistory,
        VNSaveRuntimeState saveRuntimeState,
        VNPlaytimeTracker playtimeTracker = null)
    {
        _bridge = bridge;
        _restarter = restarter;
        _dispatcher = dispatcher;
        _linePresentationAborter = linePresentationAborter;
        _lineAdvanceState = lineAdvanceState;
        _rollbackHistory = rollbackHistory;
        _saveRuntimeState = saveRuntimeState;
        _playtimeTracker = playtimeTracker;
    }

    public void PrepareForLoad()
    {
        if (_isSeeking)
        {
            Debug.LogWarning("[VNLoadSeekDriver] PrepareForLoad ignored. Already seeking.");
            return;
        }

        // LoadService가 flags 복원 전에 호출한다.
        // 따라서 여기서는 현재 실행본 정리와 UI suppression 준비만 한다.
        _isSeeking = true;
        _saveRuntimeState?.SetLoadSeeking(true);

        // 현재 세션의 rollback history는 이전 실행의 것이므로 버린다.
        // Load seek 중 새로 지나가는 라인들이 다시 history를 구성하게 하는 편이 자연스럽다.
        _rollbackHistory?.ClearRollbackHistory();

        // AdvanceGate가 기존 typewriter/line 상태를 보고 현재 line을 완료로 오판하지 않게 한다.
        // 이름은 RollbackSeek지만 기능적으로는 "seek 중 advance 잠금"이다.
        _lineAdvanceState?.MarkRollbackSeekLineEntered();

        // 현재 Presenter 실행본과 현재 DialogueBox를 닫는다.
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();
    }

    public void BeginSeek(
        VNSaveData saveData,
        Action onComplete,
        Action onFail)
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

        _target = saveData;
        _hasTarget = true;
        _onComplete = onComplete;
        _onFail = onFail;
        _matchedLineCountInNode = 0;

        Subscribe();

        // 여기서부터 CustomLinePresenter / UI suppression / command policy가
        // "지금은 load seek 중"임을 알 수 있어야 한다.
        _lineAdvanceState?.MarkRollbackSeekStarted(saveData.lineId);

        Debug.Log(
            $"[VNLoadSeekDriver] Begin seek. " +
            $"slot='{saveData.slotId}', node='{saveData.nodeName}', line='{saveData.lineId}', visitedIndex={saveData.visitedIndex}");

        _restarter.RestartNodeForLoad(saveData.nodeName);
    }

    public void OnLoadComplete(VNSaveData saveData)
    {
        if (saveData != null && _playtimeTracker != null)
            _playtimeTracker.ResumeFromSave(saveData.playtimeSeconds);

        _saveRuntimeState?.SetLoadSeeking(false);

        // target line은 이제 정상 표시되어야 한다.
        // 다만 실제 Clear는 Presenter가 target line을 소비한 뒤 하는 구조가 더 안전할 수 있다.
        // MVP에서는 여기서 Clear해도 된다.
        _lineAdvanceState?.ClearRollbackSeek();

        Debug.Log($"[VNLoadSeekDriver] OnLoadComplete. slot='{saveData?.slotId}'");
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
            MarkTargetReady();
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

        // lineId가 비어 있으면 node 시작 지점으로 load하는 정책.
        // 보통은 첫 LineEntered에서 완료된다.
        if (string.IsNullOrWhiteSpace(_target.lineId))
            return true;

        if (meta.lineId != _target.lineId)
            return false;

        // MVP에서는 0이면 첫 매칭을 target으로 본다.
        if (_target.lineVisitCountInNode <= 0)
            return true;

        _matchedLineCountInNode++;
        return _matchedLineCountInNode >= _target.lineVisitCountInNode;
    }

    private void MarkTargetReady()
    {
        // 아직 target line을 "끝까지 출력한 것"이 아니다.
        // 다음 CustomLinePresenter.RunLineAsync가 이 target line을 정상 표시해야 한다.
        _lineAdvanceState?.MarkRollbackTargetLineReady();
    }

    private void Complete()
    {
        Action callback = _onComplete;

        CleanupInternalState(keepContextForTargetDisplay: true);

        callback?.Invoke();
    }

    private void Fail(Action fallback = null)
    {
        Action callback = fallback ?? _onFail;

        CleanupInternalState(keepContextForTargetDisplay: false);

        callback?.Invoke();
    }

    private void CleanupInternalState(bool keepContextForTargetDisplay)
    {
        Unsubscribe();

        _isSeeking = false;
        _hasTarget = false;
        _target = null;

        _onComplete = null;
        _onFail = null;

        _matchedLineCountInNode = 0;

        _saveRuntimeState?.SetLoadSeeking(false);

        if (!keepContextForTargetDisplay)
            _lineAdvanceState?.ClearRollbackSeek();
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