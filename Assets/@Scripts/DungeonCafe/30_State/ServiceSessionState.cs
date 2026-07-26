using System.Collections.Generic;

/// <summary>
/// 접객 1회의 진행 상태.
/// 세션이 끝나면 ServiceSettlementCalculator 가 이 객체만 보고 결산을 만든다.
/// </summary>
public sealed class ServiceSessionState
{
    private readonly List<ServiceReactionRecord> _records = new();

    public MaidRuntimeState Maid { get; }
    public MonsterEncounterState Encounter { get; }
    public ServiceScenario Scenario { get; }

    /// <summary>종족 규약. 통제 상실 이후의 자동 사건과 회수 가능 여부를 결정한다.</summary>
    public SpeciesProtocol SpeciesProtocol { get; }

    public int DayNumber { get; }

    public ServiceSessionPhase Phase { get; private set; } = ServiceSessionPhase.None;
    public ControlAuthorityStatus ControlStatus { get; private set; } = ControlAuthorityStatus.Delegated;

    public ServiceBeat CurrentBeat { get; private set; }
    public int ConsumedBeatCount { get; private set; }

    /// <summary>통제 신호가 거부된 축. 통제 상실 전에는 의미가 없다.</summary>
    public BurdenAxis ControlLossAxis { get; private set; }

    public AxisTriple AccumulatedRawLoad { get; private set; }
    public AxisTriple AccumulatedAppliedLoad { get; private set; }

    public IReadOnlyList<ServiceReactionRecord> Records => _records;

    public bool IsControlLost => ControlStatus == ControlAuthorityStatus.Lost;

    public bool IsScenarioExhausted => ConsumedBeatCount >= Scenario.BeatBudget;

    public MonsterProfile Monster => Encounter.Monster;

    public ServiceSessionState(
        MaidRuntimeState maid,
        MonsterEncounterState encounter,
        ServiceScenario scenario,
        SpeciesProtocol speciesProtocol,
        int dayNumber)
    {
        Maid = maid;
        Encounter = encounter;
        Scenario = scenario;
        SpeciesProtocol = speciesProtocol;
        DayNumber = dayNumber;
    }

    public void SetCurrentBeat(ServiceBeat beat)
    {
        CurrentBeat = beat;
    }

    public void MarkBeatConsumed()
    {
        ConsumedBeatCount++;
    }

    public void SetControlStatus(ControlAuthorityStatus status, BurdenAxis breachAxis)
    {
        ControlStatus = status;

        if (status == ControlAuthorityStatus.Lost)
            ControlLossAxis = breachAxis;
    }

    public void RecordReaction(ServiceReactionRecord record)
    {
        _records.Add(record);

        AccumulatedRawLoad += record.RawLoad;
        AccumulatedAppliedLoad += record.AppliedLoad;
    }

    /// <summary>기본 반응 점수 합계. 결산 배율을 곱하기 전 값이다.</summary>
    public int TotalReactionScore()
    {
        int total = 0;

        for (int i = 0; i < _records.Count; i++)
            total += _records[i].ReactionScore;

        return total;
    }

    public int CountReaction(MonsterReactionGrade grade)
    {
        int count = 0;

        for (int i = 0; i < _records.Count; i++)
        {
            if (_records[i].Reaction == grade)
                count++;
        }

        return count;
    }
}
