/// <summary>
/// SyncGatePlan을 만든다.
///
/// 이 클래스는 forward modifier를 소유한다.
/// - hold
/// - extra advance
/// - suppress next base advance
///
/// DialogueRunner, RequestNextLine, lane ready 여부는 직접 다루지 않는다.
/// </summary>
public sealed class SyncGatePlanBuilder
{
    private int _holdRemainingLines;
    private int _extraAdvanceCount;
    private bool _suppressNextBaseAdvance;

    public SyncGatePlan BuildForwardPlan(
        bool canAdvance,
        int currentForwardSettleEpoch)
    {
        SyncGatePlan plan = new();

        if (!canAdvance)
            return plan;

        int advanceCount = ConsumeForwardAdvanceCount();

        if (advanceCount <= 0)
        {
            plan.Add(SyncGateToken.Immediately());
            return plan;
        }

        plan.AddRepeated(
            SyncGateToken.DispatchAdvance(SyncAdvanceKind.Scripted),
            advanceCount);

        int targetEpoch = currentForwardSettleEpoch + advanceCount;

        plan.Add(SyncGateToken.WaitForwardSettled(targetEpoch));

        return plan;
    }

    public SyncGatePlan BuildSeekResyncPlan(bool canAdvance)
    {
        SyncGatePlan plan = new();

        if (!canAdvance)
            return plan;

        ClearForwardOnlyModifiers();

        plan.Add(SyncGateToken.DispatchAdvance(SyncAdvanceKind.SeekResync));

        return plan;
    }

    public SyncGatePlan BuildManualStepPlan(
        bool canAdvance,
        int steps)
    {
        SyncGatePlan plan = new();

        if (!canAdvance)
            return plan;

        if (steps <= 0)
            steps = 1;

        plan.AddRepeated(
            SyncGateToken.DispatchAdvance(SyncAdvanceKind.ManualBypassPause),
            steps);

        return plan;
    }

    public void Hold(int lines)
    {
        if (lines < 0)
            lines = 0;

        _holdRemainingLines = lines;
    }

    public void AddExtraAdvance(int steps)
    {
        if (steps <= 0)
            return;

        _extraAdvanceCount += steps;
    }

    public void SetSuppressNextBaseAdvance(bool suppress)
    {
        _suppressNextBaseAdvance = suppress;
    }

    public void ClearForReplayBoundary()
    {
        ClearForwardOnlyModifiers();
    }

    public void Reset()
    {
        ClearForwardOnlyModifiers();
    }

    private int ConsumeForwardAdvanceCount()
    {
        if (_suppressNextBaseAdvance)
        {
            _suppressNextBaseAdvance = false;
            return ConsumeExtraAdvanceOnly();
        }

        if (_holdRemainingLines > 0)
        {
            _holdRemainingLines--;
            return ConsumeExtraAdvanceOnly();
        }

        int count = 1 + _extraAdvanceCount;
        _extraAdvanceCount = 0;

        return count;
    }

    private int ConsumeExtraAdvanceOnly()
    {
        int count = _extraAdvanceCount;
        _extraAdvanceCount = 0;

        return count;
    }

    private void ClearForwardOnlyModifiers()
    {
        _holdRemainingLines = 0;
        _extraAdvanceCount = 0;
        _suppressNextBaseAdvance = false;
    }
}