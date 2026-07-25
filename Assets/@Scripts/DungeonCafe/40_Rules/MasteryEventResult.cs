/// <summary>
/// 밤에 소화한 업무 숙련 이벤트의 결과.
/// 이벤트의 중심은 몬스터와의 관계가 아니라, 남은 영향을 관리자와 메이드가 어떻게 다루는지다.
/// </summary>
public sealed class MasteryEventResult
{
    public string MaidId { get; private set; }
    public BurdenAxis Axis { get; private set; }

    public int LevelBefore { get; private set; }
    public int LevelAfter { get; private set; }
    public int Experience { get; private set; }
    public int Threshold { get; private set; }

    public string EventNodeName { get; private set; }
    public bool IsLevelUpCommitted { get; private set; }

    public static MasteryEventResult NotReady(string maidId, BurdenAxis axis)
    {
        return new MasteryEventResult
        {
            MaidId = maidId,
            Axis = axis,
            IsLevelUpCommitted = false,
        };
    }

    public static MasteryEventResult Committed(
        string maidId,
        BurdenAxis axis,
        int levelBefore,
        int levelAfter,
        int experience,
        int threshold,
        string eventNodeName)
    {
        return new MasteryEventResult
        {
            MaidId = maidId,
            Axis = axis,
            LevelBefore = levelBefore,
            LevelAfter = levelAfter,
            Experience = experience,
            Threshold = threshold,
            EventNodeName = eventNodeName,
            IsLevelUpCommitted = true,
        };
    }
}
