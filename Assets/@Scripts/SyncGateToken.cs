public enum SyncGateTokenType
{
    Immediately,
    WaitPresentationLaneOpen,
    DispatchPresentationAdvance,
    WaitPresentationForwardSettled,
}

public enum SyncAdvanceKind
{
    Scripted,
    SeekResync,
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

    public bool CountsForForwardSettle 
        => Type == SyncGateTokenType.DispatchPresentationAdvance && 
           AdvanceKind == SyncAdvanceKind.Scripted;
    
    public static SyncGateToken WaitLaneOpen()
    => new(SyncGateTokenType.WaitPresentationLaneOpen, default, 0);

    public static SyncGateToken DispatchAdvance(SyncAdvanceKind kind)
    => new(SyncGateTokenType.DispatchPresentationAdvance, kind, 0);

    public static SyncGateToken WaitForwardSettled(int targetEpoch)
    => new(SyncGateTokenType.WaitPresentationForwardSettled, default, targetEpoch);
}