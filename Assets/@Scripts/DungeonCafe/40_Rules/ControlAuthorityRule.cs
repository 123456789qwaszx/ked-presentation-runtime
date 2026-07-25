/// <summary>
/// 접객 관리 프로토콜의 통제 권한 판정.
/// 축 하나라도 붕괴 한계에 도달하면 관리자 통제 신호가 거부된다.
/// </summary>
public static class ControlAuthorityRule
{
    public static ControlAuthorityStatus Evaluate(
        MaidBurdenState burden,
        ProgressionTuning tuning,
        out BurdenAxis breachAxis)
    {
        if (burden.TryFindLimitBreachAxis(out breachAxis))
            return ControlAuthorityStatus.Lost;

        return burden.HighestPercentOfLimit() >= tuning.StrainedThresholdPercent
            ? ControlAuthorityStatus.Strained
            : ControlAuthorityStatus.Delegated;
    }

    /// <summary>플레이어의 승인 입력이 아직 세션에 반영되는 상태인지 여부.</summary>
    public static bool AcceptsApproval(ControlAuthorityStatus status)
        => status != ControlAuthorityStatus.Lost;
}
