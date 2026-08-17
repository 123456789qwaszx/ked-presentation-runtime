
namespace Ked.Presentation.Sync
{
    // The work this token performs.
    public enum SyncGateTokenType
    {
        // Wait until the presentation lane is open to receive a main advance.
        WaitPresentationLaneOpen, 
        
        // Check that the lane is ready for advance, then call RequestNextLine().
        DispatchPresentationAdvance,
        
        // After dispatching a forward advance, wait until the forward settle epoch reaches the target.
        WaitPresentationForwardSettled,
    }

    public enum SyncAdvanceKind
    {
        None = 0,
        ScriptedForward,
        SeekResync,
    }

    public readonly struct SyncGateToken
    {
        private const int NoTargetEpoch = -1;
        
        public readonly SyncGateTokenType Type;
        public readonly SyncAdvanceKind AdvanceKind; // Meaningful only when Type is DispatchPresentationAdvance.
        public readonly int TargetEpoch; // Meaningful only when Type is WaitPresentationForwardSettled.

        private SyncGateToken(
            SyncGateTokenType type,
            SyncAdvanceKind advanceKind,
            int targetEpoch)
        {
            Type = type;
            AdvanceKind = advanceKind;
            TargetEpoch = targetEpoch;
        }
        
        public static SyncGateToken WaitLaneOpen()
            => new(SyncGateTokenType.WaitPresentationLaneOpen, SyncAdvanceKind.None, NoTargetEpoch);
        
        public static SyncGateToken DispatchAdvance(SyncAdvanceKind kind) 
            => new(SyncGateTokenType.DispatchPresentationAdvance, kind, NoTargetEpoch);

        public static SyncGateToken WaitForwardSettled(int targetEpoch) 
            => new(SyncGateTokenType.WaitPresentationForwardSettled, SyncAdvanceKind.None, targetEpoch);
    }
}