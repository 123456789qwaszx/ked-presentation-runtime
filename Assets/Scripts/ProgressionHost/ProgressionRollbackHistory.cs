using Ked.Progression;

// 진행 런타임이 rollback에서 필요로 하는 것은 좌표 하나뿐이다.
// nodeName / lineId / occurrence는 Presentation 시크에 남는다.
//
// ⚠ RollbackHistory(장면 안 좌표)와 BacklogRecorder(회차 연속 좌표)는 다른 것이다.
//   하나의 history adapter로 합치지 않는다.
public sealed class ProgressionRollbackHistory : IRollbackHistory
{
    private readonly RollbackHistory _history;

    public ProgressionRollbackHistory(RollbackHistory history)
    {
        _history = history;
    }

    public int LastHistoryIndex => _history.LastHistoryIndex;

    public bool TryTakeRollbackTarget(out int historyIndex)
    {
        if (_history.TakeRollbackTarget(out RollbackPoint target))
        {
            historyIndex = target.historyIndex;
            return true;
        }

        historyIndex = -1;
        return false;
    }
}
