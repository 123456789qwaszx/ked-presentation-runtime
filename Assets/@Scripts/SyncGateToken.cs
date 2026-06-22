public enum SyncGatePausePolicy
{
    RespectPause,
    IgnorePause,
}

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
    public readonly SyncGatePausePolicy PausePolicy;

    private SyncGateToken(
        SyncGateTokenType type,
        SyncAdvanceKind advanceKind,
        int targetForwardSettleEpoch,
        SyncGatePausePolicy pausePolicy)
    {
        Type = type;
        AdvanceKind = advanceKind;
        TargetForwardSettleEpoch = targetForwardSettleEpoch;
        PausePolicy = pausePolicy;
    }

    public bool IgnoresPause => PausePolicy == SyncGatePausePolicy.IgnorePause;

    public bool CanBypassPause
    {
        get
        {
            return Type == SyncGateTokenType.DispatchPresentationAdvance &&
                   (AdvanceKind == SyncAdvanceKind.ManualBypassPause ||
                    AdvanceKind == SyncAdvanceKind.SeekResync);
        }
    }

    public bool CountsForForwardSettle
    {
        get
        {
            return Type == SyncGateTokenType.DispatchPresentationAdvance &&
                   AdvanceKind == SyncAdvanceKind.Scripted;
        }
    }

    public static SyncGateToken Immediately()
    {
        return new SyncGateToken(
            SyncGateTokenType.Immediately,
            default,
            0,
            SyncGatePausePolicy.RespectPause);
    }

    public static SyncGateToken WaitLaneOpen()
    {
        return new SyncGateToken(
            SyncGateTokenType.WaitPresentationLaneOpen,
            default,
            0,
            SyncGatePausePolicy.RespectPause);
    }

    public static SyncGateToken WaitLaneOpenIgnoringPause()
    {
        return new SyncGateToken(
            SyncGateTokenType.WaitPresentationLaneOpen,
            default,
            0,
            SyncGatePausePolicy.IgnorePause);
    }

    public static SyncGateToken DispatchAdvance(SyncAdvanceKind kind)
    {
        SyncGatePausePolicy pausePolicy =
            kind == SyncAdvanceKind.SeekResync ||
            kind == SyncAdvanceKind.ManualBypassPause
                ? SyncGatePausePolicy.IgnorePause
                : SyncGatePausePolicy.RespectPause;

        return new SyncGateToken(
            SyncGateTokenType.DispatchPresentationAdvance,
            kind,
            0,
            pausePolicy);
    }

    public static SyncGateToken WaitForwardSettled(int targetEpoch)
    {
        return new SyncGateToken(
            SyncGateTokenType.WaitPresentationForwardSettled,
            default,
            targetEpoch,
            SyncGatePausePolicy.RespectPause);
    }
}