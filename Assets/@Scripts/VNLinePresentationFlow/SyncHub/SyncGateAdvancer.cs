public enum SyncGateAdvanceResult
{
    Completed,
    Progressed,
    Blocked,
    LaneClosed
}

public sealed class SyncGateAdvancer
{
    public SyncGateAdvanceResult TryAdvanceCurrent(
        SyncGateState gate,
        PresentationLaneState lane)
    {
        SyncGateToken? tokenOpt = gate.CurrentToken;

        if (!tokenOpt.HasValue)
            return SyncGateAdvanceResult.Completed;

        SyncGateToken token = tokenOpt.Value;

        switch (token.Type)
        {
            case SyncGateTokenType.WaitPresentationLaneOpen:
                return TryConsumeWaitLaneOpen(gate, lane);

            case SyncGateTokenType.DispatchPresentationAdvance:
                return TryConsumeDispatchAdvance(gate, lane, token.AdvanceKind);

            case SyncGateTokenType.WaitPresentationForwardSettled:
                return TryConsumeWaitForwardSettled(gate, lane, token.TargetEpoch);

            default:
                return SyncGateAdvanceResult.Blocked;
        }
    }

    private SyncGateAdvanceResult TryConsumeWaitLaneOpen(
        SyncGateState gate,
        PresentationLaneState lane)
    {
        if (!lane.IsAvailable)
            return SyncGateAdvanceResult.LaneClosed;

        if (!lane.IsOpenForMain)
            return SyncGateAdvanceResult.Blocked;

        gate.ConsumeCurrent();
        return SyncGateAdvanceResult.Progressed;
    }

    private SyncGateAdvanceResult TryConsumeDispatchAdvance(
        SyncGateState gate,
        PresentationLaneState lane,
        SyncAdvanceKind advanceKind)
    {
        if (!lane.IsAvailable)
            return SyncGateAdvanceResult.LaneClosed;

        if (!lane.IsReadyForAdvance)
            return SyncGateAdvanceResult.Blocked;

        gate.ConsumeCurrent();
        lane.MarkAdvanceDispatched(advanceKind);

        if (!lane.IsDialogueRunning)
        {
            lane.CompleteRun();
            return SyncGateAdvanceResult.LaneClosed;
        }

        lane.RequestNextLine();
        return SyncGateAdvanceResult.Progressed;
    }

    private SyncGateAdvanceResult TryConsumeWaitForwardSettled(
        SyncGateState gate,
        PresentationLaneState lane,
        int targetEpoch)
    {
        if (lane.ForwardSettleEpoch >= targetEpoch)
        {
            gate.ConsumeCurrent();
            return SyncGateAdvanceResult.Progressed;
        }

        if (!lane.IsAvailable)
            return SyncGateAdvanceResult.LaneClosed;

        return SyncGateAdvanceResult.Blocked;
    }
}