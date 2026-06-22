public enum SyncGateAdvanceResult
{
    Completed,
    Progressed,
    Blocked,
    LaneCompleted,
    LanePaused,
    LaneUnavailable,
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
            case SyncGateTokenType.Immediately:
                gate.ConsumeCurrent();
                return SyncGateAdvanceResult.Progressed;

            case SyncGateTokenType.WaitPresentationLaneOpen:
                return TryConsumeWaitLaneOpen(gate, lane, token);

            case SyncGateTokenType.DispatchPresentationAdvance:
                return TryConsumeDispatchAdvance(gate, lane, token);

            case SyncGateTokenType.WaitPresentationForwardSettled:
                return TryConsumeWaitForwardSettled(
                    gate,
                    lane,
                    token.TargetForwardSettleEpoch);

            default:
                return SyncGateAdvanceResult.Blocked;
        }
    }

    private SyncGateAdvanceResult TryConsumeWaitLaneOpen(
        SyncGateState gate,
        PresentationLaneState lane,
        SyncGateToken token)
    {
        if (lane.IsCompleted)
            return SyncGateAdvanceResult.LaneCompleted;

        if (!lane.IsAvailable)
            return SyncGateAdvanceResult.LaneUnavailable;

        if (lane.IsPaused && !token.IgnoresPause)
            return SyncGateAdvanceResult.LanePaused;

        if (!lane.IsOpenForMain)
            return SyncGateAdvanceResult.Blocked;

        gate.ConsumeCurrent();
        return SyncGateAdvanceResult.Progressed;
    }

    private SyncGateAdvanceResult TryConsumeDispatchAdvance(
        SyncGateState gate,
        PresentationLaneState lane,
        SyncGateToken token)
    {
        if (lane.IsCompleted)
            return SyncGateAdvanceResult.LaneCompleted;

        if (!lane.IsAvailable)
            return SyncGateAdvanceResult.LaneUnavailable;

        if (!lane.IsReadyForAdvance)
            return SyncGateAdvanceResult.Blocked;

        if (lane.IsPaused && !token.CanBypassPause)
            return SyncGateAdvanceResult.Blocked;

        if (!gate.TryConsumeCurrent(out SyncGateToken consumed))
            return SyncGateAdvanceResult.Blocked;

        lane.MarkAdvanceDispatched(consumed);

        if (!lane.IsDialogueRunning)
        {
            lane.CompleteRun();
            return SyncGateAdvanceResult.LaneCompleted;
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

        if (lane.IsCompleted)
            return SyncGateAdvanceResult.LaneCompleted;

        if (!lane.IsAvailable)
            return SyncGateAdvanceResult.LaneUnavailable;

        if (lane.IsPaused)
            return SyncGateAdvanceResult.LanePaused;

        return SyncGateAdvanceResult.Blocked;
    }
}