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

    // 디버그/강제 진행 전용.
    // inline advance는 이걸 쓰면 안 된다.
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