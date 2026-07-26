/// <summary>
/// 접객 1회의 결산 결과. 결과창과 하루 리포트가 이 객체만 읽는다.
/// 생성은 ServiceSettlementCalculator 에서만 한다.
/// </summary>
public sealed class ServiceSettlementResult
{
    public string MaidId { get; private set; }
    public string MaidDisplayName { get; private set; }
    public string MonsterId { get; private set; }
    public string MonsterDisplayName { get; private set; }
    public MonsterSpecies Species { get; private set; }

    public int DayNumber { get; private set; }
    public int SlotIndex { get; private set; }

    // ---- 반응 ----
    public int GreatlySatisfiedCount { get; private set; }
    public int SatisfiedCount { get; private set; }
    public int NoResponseCount { get; private set; }
    public int BaseReactionScore { get; private set; }

    // ---- 배율 ----
    public BurdenAxis DemandAxis { get; private set; }
    public int DemandCollapse { get; private set; }
    public float Multiplier { get; private set; }
    public string MultiplierLabel { get; private set; }

    // ---- 산출 ----
    public int Energy { get; private set; }

    // ---- 예약 성사 ----
    public int Satisfaction { get; private set; }
    public int RequiredSatisfaction { get; private set; }
    public bool IsSatisfactionMet { get; private set; }

    // ---- 성장/사고 ----
    public AxisTriple MasteryGain { get; private set; }
    public AxisTriple BurdenAfter { get; private set; }
    public AxisTriple ResidualBurden { get; private set; }
    public bool IsIncident { get; private set; }
    public BurdenAxis ControlLossAxis { get; private set; }
    public bool IsMaidLost { get; private set; }

    public static ServiceSettlementResult Create(
        ServiceSessionState session,
        int greatlySatisfiedCount,
        int satisfiedCount,
        int noResponseCount,
        int baseReactionScore,
        CollapseMultiplierBand band,
        int demandCollapse,
        int energy,
        AxisTriple masteryGain,
        AxisTriple residualBurden,
        bool isMaidLost)
    {
        return new ServiceSettlementResult
        {
            MaidId = session.Maid.MaidId,
            MaidDisplayName = session.Maid.DisplayName,
            MonsterId = session.Monster.MonsterId,
            MonsterDisplayName = session.Monster.DisplayName,
            Species = session.Monster.Species,

            DayNumber = session.DayNumber,

            GreatlySatisfiedCount = greatlySatisfiedCount,
            SatisfiedCount = satisfiedCount,
            NoResponseCount = noResponseCount,
            BaseReactionScore = baseReactionScore,

            DemandAxis = session.Monster.DemandAxis,
            DemandCollapse = demandCollapse,
            Multiplier = band.Multiplier,
            MultiplierLabel = band.Label,

            Energy = energy,

            Satisfaction = session.Encounter.Satisfaction,
            RequiredSatisfaction = session.Encounter.RequiredSatisfaction,
            IsSatisfactionMet = session.Encounter.IsRequirementMet,

            MasteryGain = masteryGain,
            BurdenAfter = session.Maid.Burden.Snapshot(),
            ResidualBurden = residualBurden,
            IsIncident = session.IsControlLost,
            ControlLossAxis = session.ControlLossAxis,
            IsMaidLost = isMaidLost,
        };
    }

    public string ToSummaryLine()
        => $"{MaidDisplayName} / {MonsterDisplayName} : " +
           $"반응 {BaseReactionScore} × {BurdenAxes.ToBurdenLabel(DemandAxis)} {DemandCollapse} " +
           $"(×{Multiplier:0.0}) = 에너지 {Energy}";
}
