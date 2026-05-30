public enum VNSeekLineDecisionKind
{
    None = 0,
    NotSeeking = 1,

    // Seeking 중인데 target이 아님.
    PassThrough = 2,

    // 현재 라인이 seek target이라 TargetLinePending으로 전환됨.
    TargetReached = 3,

    // 현재 lineId가 pending target line임. 표시 후 consume 필요.
    PendingTargetLine = 4,
}