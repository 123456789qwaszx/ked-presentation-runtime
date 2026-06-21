public enum SyncGateTokenType
{
    // No wait. Consumed immediately.
    Immediately,

    // Wait until the presentation lane is ready or released.
    // Ready means the side lane can receive RequestNextLine.
    // Released means the current side line was torn down and main should not remain blocked.
    WaitPresentationLaneOpen,

    // Dispatch one side-lane advance.
    // This token is only consumed when the lane gate allows it.
    DispatchPresentationAdvance,

    // Wait until ForwardSettleEpoch reaches the target epoch.
    WaitPresentationForwardSettled,
}

public enum SyncAdvanceKind
{
    // Deterministic forward-play advance.
    // Counts for forward-settle accounting.
    Scripted,

    // Seek/pass-through resync advance.
    // Moves the side lane but does not count as normal forward-play settle.
    SeekResync,

    // Explicit manual advance.
    // May bypass pause. Does not count for forward-settle accounting.
    ManualBypassPause,
}

public readonly struct SyncGateToken
{
    public readonly SyncGateTokenType Type;
    public readonly SyncAdvanceKind AdvanceKind;
    public readonly int TargetForwardSettleEpoch;

    private SyncGateToken(
        SyncGateTokenType type,
        SyncAdvanceKind advanceKind,
        int targetForwardSettleEpoch)
    {
        Type = type;
        AdvanceKind = advanceKind;
        TargetForwardSettleEpoch = targetForwardSettleEpoch;
    }

    public bool CanBypassPause => AdvanceKind == SyncAdvanceKind.ManualBypassPause;
    public bool CountsForForwardSettle => AdvanceKind == SyncAdvanceKind.Scripted;

    public static SyncGateToken Immediately()
    {
        return new SyncGateToken(
            SyncGateTokenType.Immediately,
            default,
            0);
    }

    public static SyncGateToken WaitLaneOpen()
    {
        return new SyncGateToken(
            SyncGateTokenType.WaitPresentationLaneOpen,
            default,
            0);
    }

    public static SyncGateToken DispatchAdvance(SyncAdvanceKind kind)
    {
        return new SyncGateToken(
            SyncGateTokenType.DispatchPresentationAdvance,
            kind,
            0);
    }

    public static SyncGateToken WaitForwardSettled(int targetEpoch)
    {
        return new SyncGateToken(
            SyncGateTokenType.WaitPresentationForwardSettled,
            default,
            targetEpoch);
    }
}