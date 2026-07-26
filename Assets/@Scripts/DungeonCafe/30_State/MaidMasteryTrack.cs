/// <summary>
/// 한 축의 업무 숙련도.
///
/// 경험치가 기준에 도달해도 레벨이 자동으로 오르지 않는다.
/// 대신 IsEventReady 가 켜지고, 밤에 숙련 이벤트를 소화해야 CommitLevelUp 으로 확정된다.
/// 임계값은 누적 기준이므로 표시는 "38 / 50" 형태 그대로 쓸 수 있다.
/// </summary>
public sealed class MaidMasteryTrack
{
    public BurdenAxis Axis { get; }

    public int Level { get; private set; }
    public int Experience { get; private set; }

    public MaidMasteryTrack(BurdenAxis axis)
    {
        Axis = axis;
    }

    public int GetNextThreshold(ProgressionTuning tuning)
        => tuning.GetMasteryThreshold(Axis, Level);

    public bool IsMaxLevel(ProgressionTuning tuning)
        => Level >= tuning.GetMaxMasteryLevel(Axis);

    /// <summary>밤에 소화할 숙련 이벤트가 준비되었는지 여부.</summary>
    public bool IsEventReady(ProgressionTuning tuning)
    {
        if (IsMaxLevel(tuning))
            return false;

        return Experience >= GetNextThreshold(tuning);
    }

    /// <summary>실제로 반영된 경험치를 반환한다.</summary>
    public int AddExperience(int amount)
    {
        if (amount <= 0)
            return 0;

        Experience += amount;
        return amount;
    }

    /// <summary>숙련 이벤트를 끝까지 진행했을 때만 호출한다.</summary>
    public bool CommitLevelUp(ProgressionTuning tuning)
    {
        if (!IsEventReady(tuning))
            return false;

        Level++;
        return true;
    }

    public override string ToString()
        => $"{BurdenAxes.ToMasteryLabel(Axis)} Lv.{Level} ({Experience})";
}
