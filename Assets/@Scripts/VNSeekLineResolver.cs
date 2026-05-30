/// <summary>
/// 현재 라인이 Seek 상태에서 어떻게 처리되어야 하는지 판정한다.
/// </summary>
public sealed class VNSeekLineResolver
{
    private readonly LinePresentationAdvanceState _advanceState;

    public VNSeekLineResolver(LinePresentationAdvanceState advanceState)
    {
        _advanceState = advanceState;
    }

    public sealed class Decision
    {
        /// <summary>
        /// Seek 중이며 이 라인은 target이 아니다 → 화면 표시 없이 즉시 통과.
        /// </summary>
        public bool ShouldPassThrough { get; set; }

        /// <summary>
        /// 이 라인이 seek target으로 pending된 상태이다.
        /// → 화면에 표시하지만 immediate transition 사용, seek 소비 필요.
        /// </summary>
        public bool IsPendingSeekTargetLine { get; set; }
    }

    /// <summary>
    /// 라인 진입 커밋 직후 호출한다.
    /// AdvanceState.MarkLineEntered() 이후여야 seek 상태가 정확하다.
    /// </summary>
    public Decision Resolve(string lineId)
    {
        bool isPending = _advanceState.IsPendingSeekTargetLine(lineId);

        // Seeking이지만 아직 pending target이 아니면 pass-through
        bool shouldPassThrough = _advanceState.IsSeeking && !isPending;

        return new Decision
        {
            ShouldPassThrough = shouldPassThrough,
            IsPendingSeekTargetLine = isPending,
        };
    }

    /// <summary>
    /// target line pending을 소비하고 seek를 완료 처리한다.
    /// </summary>
    public void ConsumeTargetLine(string lineId)
    {
        _advanceState.ConsumeSeekTargetLine(lineId);
    }
}