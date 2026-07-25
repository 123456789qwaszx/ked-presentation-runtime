/// <summary>
/// 접객 1회 동안의 몬스터 측 상태.
/// 만족도 게이지는 예약 성공 판정에만 쓰이고, 에너지 산출은 결산의 반응 점수를 따른다.
/// </summary>
public sealed class MonsterEncounterState
{
    public MonsterProfile Monster { get; }

    public int Satisfaction { get; private set; }

    public MonsterEncounterState(MonsterProfile profile)
    {
        Monster = profile;
    }

    public int RequiredSatisfaction => Monster.RequiredSatisfaction;
    public int MaxSatisfaction => Monster.MaxSatisfaction;

    public bool IsRequirementMet => Satisfaction >= Monster.RequiredSatisfaction;

    public int SatisfactionPercent
        => MaxSatisfaction <= 0 ? 0 : Satisfaction * 100 / MaxSatisfaction;

    /// <summary>실제로 차오른 만족도를 반환한다.</summary>
    public int ApplyReaction(MonsterReactionGrade reaction, int bonus)
    {
        int gain = reaction.ToReactionScore() * Monster.SatisfactionPerScore + bonus;

        if (gain <= 0)
            return 0;

        int before = Satisfaction;
        int after = System.Math.Min(MaxSatisfaction, before + gain);

        Satisfaction = after;

        return after - before;
    }
}
