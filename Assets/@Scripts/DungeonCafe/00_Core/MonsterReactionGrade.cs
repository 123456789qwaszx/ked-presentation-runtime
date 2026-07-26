/// <summary>
/// 승인된 행동 하나에 대한 몬스터의 반응.
/// enum 값 자체가 결산에서 사용하는 기본 반응 점수다.
/// </summary>
public enum MonsterReactionGrade
{
    // 반응하지 않는다.
    NoResponse = 0,

    // 만족했다.
    Satisfied = 1,

    // 크게 만족했다.
    GreatlySatisfied = 3,
}

public static class MonsterReactionGradeExtensions
{
    public static int ToReactionScore(this MonsterReactionGrade grade) => (int)grade;

    public static string ToLabel(this MonsterReactionGrade grade) => grade switch
    {
        MonsterReactionGrade.GreatlySatisfied => "크게 만족했다",
        MonsterReactionGrade.Satisfied => "만족했다",
        _ => "반응하지 않는다",
    };
}
