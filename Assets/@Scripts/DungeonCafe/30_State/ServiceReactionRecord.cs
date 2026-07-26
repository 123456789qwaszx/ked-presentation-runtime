/// <summary>
/// 승인된 행동 하나의 결과 기록. 결산과 로그, 결과창 연출이 이 목록만 읽는다.
/// </summary>
public readonly struct ServiceReactionRecord
{
    public readonly string BeatKey;
    public readonly string OptionKey;
    public readonly MonsterReactionGrade Reaction;

    /// <summary>대응력 완화 전 원본 부하. 숙련 경험치 산출에 사용한다.</summary>
    public readonly AxisTriple RawLoad;

    /// <summary>실제로 붕괴도에 누적된 부하.</summary>
    public readonly AxisTriple AppliedLoad;

    public readonly int SatisfactionGained;

    /// <summary>통제 상실 이후 자동 진행으로 발생한 기록인지 여부.</summary>
    public readonly bool IsAutonomous;

    public ServiceReactionRecord(
        string beatKey,
        string optionKey,
        MonsterReactionGrade reaction,
        AxisTriple rawLoad,
        AxisTriple appliedLoad,
        int satisfactionGained,
        bool isAutonomous)
    {
        BeatKey = beatKey;
        OptionKey = optionKey;
        Reaction = reaction;
        RawLoad = rawLoad;
        AppliedLoad = appliedLoad;
        SatisfactionGained = satisfactionGained;
        IsAutonomous = isAutonomous;
    }

    public int ReactionScore => Reaction.ToReactionScore();
}
