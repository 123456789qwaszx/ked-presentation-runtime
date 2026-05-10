using System;
using UnityEngine;

public sealed class RollbackController : IDisposable
{
    private readonly RollbackHistory _history;
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly LinePresentationAdvanceState _lineAdvanceState;

    public RollbackController(
        RollbackHistory history,
        YarnLineLifecycleBridge bridge,
        DialogueAdvanceDispatcher dispatcher,
        LinePresentationAdvanceState lineAdvanceState)
    {
        _history = history;
        _bridge = bridge;
        _dispatcher = dispatcher;
        _lineAdvanceState = lineAdvanceState;

        _bridge.LineEntered -= HandleLineEnteredDuringRollbackSeek;
        _bridge.LineEntered += HandleLineEnteredDuringRollbackSeek;

        _bridge.LineEntered -= AddRollbackPoint;
        _bridge.LineEntered += AddRollbackPoint;
    }

    public bool RequestRollbackOneStep()
    {
        Debug.Log($"[Rollback] Request one step. historyCount={_history.Points.Count}, active={_lineAdvanceState.IsRollbackActive}");

        // Rollback seek 중이거나,
        // target line에 도달했지만 아직 Presenter가 그 target line을 소비하지 않은 상태라면
        // 추가 rollback 요청을 받으면 안 된다.
        //
        // 그렇지 않으면 한 번 클릭했는데도 TryPrepareRollbackOneStep()이 여러 번 실행되어
        // history가 2칸, 4칸씩 잘리는 문제가 생길 수 있다.
        if (_lineAdvanceState.IsRollbackActive)
        {
            Debug.Log("[Rollback] Ignored. Rollback is already active.");
            return false;
        }

        if (!_history.TryPrepareRollbackOneStep(out RollbackPoint target))
        {
            Debug.Log("[Rollback] Ignored. Not enough rollback history.");
            return false;
        }

        Debug.Log(
            $"[Rollback] Prepared. targetNode={target.nodeName}, targetLine={target.lineId}, historyCountAfterTrim={_history.Points.Count}");

        // 이 시점부터 LineEntered로 들어오는 중간 라인들은
        // history에 기록하면 안 된다.
        //
        // target line에 도달할 때까지 Yarn을 자동 진행시키고,
        // target line에 도달하면 pending 상태로 전환한다.
        _lineAdvanceState.BeginRollbackSeek(
            target.nodeName,
            target.lineId);

        return true;
    }

    public bool RequestRollbackToHistoryIndex(int historyIndex)
    {
        Debug.Log(
            $"[Rollback] Request history index. index={historyIndex}, historyCount={_history.Points.Count}, active={_lineAdvanceState.IsRollbackActive}");

        if (_lineAdvanceState.IsRollbackActive)
        {
            Debug.Log("[Rollback] Ignored. Rollback is already active.");
            return false;
        }

        if (!_history.TryPrepareRollbackToHistoryIndex(historyIndex, out RollbackPoint target))
        {
            Debug.Log("[Rollback] Ignored. Invalid rollback history index.");
            return false;
        }

        Debug.Log(
            $"[Rollback] Prepared by index. targetNode={target.nodeName}, targetLine={target.lineId}, historyCountAfterTrim={_history.Points.Count}");

        _lineAdvanceState.BeginRollbackSeek(
            target.nodeName,
            target.lineId);

        return true;
    }

    private void HandleLineEnteredDuringRollbackSeek(YarnLineMeta meta)
    {
        // Rollback seek 중이 아니면 이 핸들러는 아무것도 하지 않는다.
        //
        // RollbackPointBlocked만 보고 판단하지 않는 이유:
        // RollbackPointBlocked는 "기록 금지"의 의미이고,
        // IsRollbackSeeking은 "target까지 자동 진행 중"이라는 의미이기 때문이다.
        if (!_lineAdvanceState.IsRollbackSeeking)
            return;

        // target line에 도달했다.
        //
        // 아직 화면에 표시된 것은 아니다.
        // 다음 CustomLinePresenter.RunLineAsync가 이 line을 받아서
        // one-shot rollback target으로 소비해야 한다.
        if (_lineAdvanceState.IsRollbackSeekTarget(meta))
        {
            Debug.Log($"[Rollback] Target line reached. node={meta.nodeName}, line={meta.lineId}");

            _lineAdvanceState.PrepareRollbackTargetLine();
            return;
        }

        // 아직 target line이 아니므로 Yarn을 한 줄 더 진행시킨다.
        //
        // 이 과정에서 들어오는 LineEntered는 AddRollbackPoint에서 막힌다.
        Debug.Log($"[Rollback] Seek next. currentNode={meta.nodeName}, currentLine={meta.lineId}");

        _dispatcher.DispatchSeekNext();
    }

    private void AddRollbackPoint(YarnLineMeta meta)
    {
        // Rollback seek 중 지나가는 라인은 history에 기록하지 않는다.
        //
        // target line 역시 Pending 상태에서는 아직 표시 전이므로 기록하지 않는다.
        // 실제 target line이 CustomLinePresenter에서 표시되고
        // ConsumeRollbackTargetLine()으로 rollback 상태가 풀린 뒤,
        // 이후 정상 흐름에서 다시 기록되는 구조를 기대한다.
        if (!_lineAdvanceState.CanRecordRollbackPoint)
            return;

        _history.AddRollbackPoint(meta);
    }

    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.LineEntered -= HandleLineEnteredDuringRollbackSeek;
        _bridge.LineEntered -= AddRollbackPoint;
    }
}