using System.Collections.Generic;

public sealed class SyncGatePlanBuilder
{
    private int _holdRemainingLines;
    private int _extraAdvanceCount;

    public SyncGatePlan ConsumeForwardPlan(
        bool canAdvance,
        int currentForwardSettleEpoch)
    {
        if (!canAdvance)
            return SyncGatePlan.Empty();

        int advanceCount = ConsumeForwardAdvanceCount();

        if (advanceCount == 0)
            return SyncGatePlan.Empty();

        List<SyncGateToken> tokens = new();

        for (int i = 0; i < advanceCount; i++)
            tokens.Add(SyncGateToken.DispatchAdvance(SyncAdvanceKind.Scripted));

        int targetEpoch = currentForwardSettleEpoch + advanceCount;
        tokens.Add(SyncGateToken.WaitForwardSettled(targetEpoch));

        return new SyncGatePlan(tokens);
    }

    public SyncGatePlan ConsumeSeekResyncPlan(bool canAdvance)
    {
        if (!canAdvance)
            return SyncGatePlan.Empty();

        int advanceCount = ConsumeForwardAdvanceCount();

        if (advanceCount == 0)
            return SyncGatePlan.Empty();

        List<SyncGateToken> tokens = new();

        for (int i = 0; i < advanceCount; i++)
            tokens.Add(SyncGateToken.DispatchAdvance(SyncAdvanceKind.SeekResync));

        tokens.Add(SyncGateToken.WaitLaneOpen());

        return new SyncGatePlan(tokens);
    }

    public SyncGatePlan BuildInlineScriptedAdvancePlan(
        bool canAdvance,
        int currentForwardSettleEpoch,
        int steps)
    {
        if (!canAdvance)
            return SyncGatePlan.Empty();

        if (steps <= 0)
            steps = 1;

        List<SyncGateToken> tokens = new();

        for (int i = 0; i < steps; i++)
            tokens.Add(SyncGateToken.DispatchAdvance(SyncAdvanceKind.Scripted));

        int targetEpoch = currentForwardSettleEpoch + steps;
        tokens.Add(SyncGateToken.WaitForwardSettled(targetEpoch));

        return new SyncGatePlan(tokens);
    }

    public void Hold(int lines)
    {
        _holdRemainingLines = lines;
    }

    public void AddExtraAdvance(int steps)
    {
        if (steps <= 0)
            return;

        _extraAdvanceCount += steps;
    }

    public void Reset()
    {
        _holdRemainingLines = 0;
        _extraAdvanceCount = 0;
    }

    private int ConsumeForwardAdvanceCount()
    {
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
}