/// <summary>
/// 접객 종료 시 한 번에 수행하는 결산.
///
/// 진행 중에는 이야기와 반응에만 집중하고, 계산은 여기서만 일어난다.
///   기본 반응 점수 합계 x (몬스터가 요구하는 축의 최종 붕괴도 배율) = 에너지
///
/// 숙련 경험치 부여와 후유증 누적도 이 시점에 함께 커밋한다.
/// </summary>
public sealed class ServiceSettlementCalculator
{
    private readonly ProgressionTuning _tuning;

    public ServiceSettlementCalculator(ProgressionTuning tuning)
    {
        _tuning = tuning;
    }

    public ServiceSettlementResult Settle(ServiceSessionState session)
    {
        MaidRuntimeState maid = session.Maid;

        int baseReactionScore = session.TotalReactionScore();

        BurdenAxis demandAxis = session.Monster.DemandAxis;
        int demandCollapse = maid.Burden.Get(demandAxis);

        CollapseMultiplierBand band = CollapseMultiplierTable.Resolve(_tuning, demandCollapse);
        int energy = CollapseMultiplierTable.ApplyToScore(baseReactionScore, band.Multiplier);

        bool isIncident = session.IsControlLost;

        AxisTriple masteryGain = MasteryExperienceRule.CalculateGain(
            session.AccumulatedRawLoad,
            isIncident,
            _tuning);

        MasteryExperienceRule.Grant(maid, masteryGain);

        AxisTriple residual = AxisTriple.Zero;

        if (isIncident)
        {
            residual = maid.Burden.Add(_tuning.IncidentResidualBurden);
            maid.MarkIncident(recovered: CanRecoverFromIncident(session));
        }

        return ServiceSettlementResult.Create(
            session,
            greatlySatisfiedCount: session.CountReaction(MonsterReactionGrade.GreatlySatisfied),
            satisfiedCount: session.CountReaction(MonsterReactionGrade.Satisfied),
            noResponseCount: session.CountReaction(MonsterReactionGrade.NoResponse),
            baseReactionScore: baseReactionScore,
            band: band,
            demandCollapse: demandCollapse,
            energy: energy,
            masteryGain: masteryGain,
            residualBurden: residual,
            isMaidLost: maid.IsLost);
    }

    /// <summary>
    /// 통제 상실 이후 회수가 가능한지는 종족 규약이 결정한다.
    /// 회수 불가 종족에서 한계를 넘기면 그 메이드는 캠페인에서 이탈한다.
    /// </summary>
    private static bool CanRecoverFromIncident(ServiceSessionState session)
        => session.SpeciesProtocol == null || session.SpeciesProtocol.AllowsWithdrawAfterControlLoss;
}
