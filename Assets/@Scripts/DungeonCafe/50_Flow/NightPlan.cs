/// <summary>
/// 밤에 실행할 처리 하나.
/// 하루에 한 명에게만 적용되므로, 누구를 어떤 방식으로 다룰지가 곧 선택이다.
/// </summary>
public readonly struct NightPlan
{
    public readonly NightProgramKind Kind;
    public readonly string MaidId;
    public readonly BurdenAxis Axis;

    public NightPlan(NightProgramKind kind, string maidId, BurdenAxis axis)
    {
        Kind = kind;
        MaidId = maidId;
        Axis = axis;
    }
}
