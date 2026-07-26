/// <summary>
/// 상시 표시 UI 가 한 번에 필요로 하는 값을 모은 스냅숏.
///
/// 오버레이가 캠페인/하루/세션 상태 객체를 직접 붙들면 표현 계층이 진행 상태에 결합된다.
/// 그래서 매 갱신마다 값 복사본을 만들어 넘기고, UI 는 이 구조체 외에는 아무것도 모른다.
/// Unity 에 의존하지 않으므로 헤드리스 구동에서도 그대로 만들어진다.
/// </summary>
public readonly struct GuesthouseHudSnapshot
{
    public readonly int DayNumber;
    public readonly int DayCount;
    public readonly int SlotIndex;
    public readonly int SlotCount;

    public readonly int EnergyToday;
    public readonly int EnergyTotal;
    public readonly int EnergyQuota;

    public readonly bool HasMaid;
    public readonly string MaidName;
    public readonly AxisTriple Burden;
    public readonly AxisTriple BurdenLimit;
    public readonly ControlAuthorityStatus ControlStatus;

    public readonly bool HasMonster;
    public readonly string MonsterName;
    public readonly BurdenAxis DemandAxis;
    public readonly bool IsDemandKnown;
    public readonly int Satisfaction;
    public readonly int RequiredSatisfaction;

    /// <summary>지금 화면에서 진행 중인 구간. 오버레이 머리말에 그대로 쓴다.</summary>
    public readonly string PhaseLabel;

    private GuesthouseHudSnapshot(
        int dayNumber,
        int dayCount,
        int slotIndex,
        int slotCount,
        int energyToday,
        int energyTotal,
        int energyQuota,
        bool hasMaid,
        string maidName,
        AxisTriple burden,
        AxisTriple burdenLimit,
        ControlAuthorityStatus controlStatus,
        bool hasMonster,
        string monsterName,
        BurdenAxis demandAxis,
        bool isDemandKnown,
        int satisfaction,
        int requiredSatisfaction,
        string phaseLabel)
    {
        DayNumber = dayNumber;
        DayCount = dayCount;
        SlotIndex = slotIndex;
        SlotCount = slotCount;

        EnergyToday = energyToday;
        EnergyTotal = energyTotal;
        EnergyQuota = energyQuota;

        HasMaid = hasMaid;
        MaidName = maidName;
        Burden = burden;
        BurdenLimit = burdenLimit;
        ControlStatus = controlStatus;

        HasMonster = hasMonster;
        MonsterName = monsterName;
        DemandAxis = demandAxis;
        IsDemandKnown = isDemandKnown;
        Satisfaction = satisfaction;
        RequiredSatisfaction = requiredSatisfaction;

        PhaseLabel = phaseLabel;
    }

    /// <summary>접객 밖의 구간. 메이드/몬스터 칸은 비운다.</summary>
    public static GuesthouseHudSnapshot ForDay(
        CampaignState campaign,
        DayCycleState day,
        string phaseLabel)
    {
        return new GuesthouseHudSnapshot(
            dayNumber: day != null ? day.DayNumber : campaign.NextDayNumber,
            dayCount: campaign.Tuning.CampaignDayCount,
            slotIndex: day != null ? day.SlotCursor : 0,
            slotCount: campaign.Tuning.ServicesPerDay,
            energyToday: day != null ? day.EnergyEarned : 0,
            energyTotal: campaign.TotalEnergy,
            energyQuota: campaign.Tuning.CampaignEnergyQuota,
            hasMaid: false,
            maidName: null,
            burden: AxisTriple.Zero,
            burdenLimit: AxisTriple.Zero,
            controlStatus: ControlAuthorityStatus.Delegated,
            hasMonster: false,
            monsterName: null,
            demandAxis: BurdenAxis.Physical,
            isDemandKnown: false,
            satisfaction: 0,
            requiredSatisfaction: 0,
            phaseLabel: phaseLabel);
    }

    /// <summary>접객 진행 중. 노드가 재생되는 동안 이 값이 계속 떠 있어야 한다.</summary>
    public static GuesthouseHudSnapshot ForSession(
        CampaignState campaign,
        DayCycleState day,
        ServiceSessionState session,
        string phaseLabel)
    {
        MaidRuntimeState maid = session.Maid;
        MonsterProfile monster = session.Encounter.Monster;

        return new GuesthouseHudSnapshot(
            dayNumber: day != null ? day.DayNumber : campaign.NextDayNumber,
            dayCount: campaign.Tuning.CampaignDayCount,
            slotIndex: day != null ? day.SlotCursor : 0,
            slotCount: campaign.Tuning.ServicesPerDay,
            energyToday: day != null ? day.EnergyEarned : 0,
            energyTotal: campaign.TotalEnergy,
            energyQuota: campaign.Tuning.CampaignEnergyQuota,
            hasMaid: true,
            maidName: maid.Profile.DisplayName,
            burden: maid.Burden.Snapshot(),
            burdenLimit: maid.Burden.LimitSnapshot(),
            controlStatus: session.ControlStatus,
            hasMonster: true,
            monsterName: monster.DisplayName,
            demandAxis: monster.DemandAxis,
            isDemandKnown: true,
            satisfaction: session.Encounter.Satisfaction,
            requiredSatisfaction: monster.RequiredSatisfaction,
            phaseLabel: phaseLabel);
    }

    /// <summary>밤 구간. 대상 메이드만 표시한다.</summary>
    public static GuesthouseHudSnapshot ForNight(
        CampaignState campaign,
        int dayNumber,
        MaidRuntimeState maid,
        string phaseLabel)
    {
        bool hasMaid = maid != null;

        return new GuesthouseHudSnapshot(
            dayNumber: dayNumber,
            dayCount: campaign.Tuning.CampaignDayCount,
            slotIndex: campaign.Tuning.ServicesPerDay,
            slotCount: campaign.Tuning.ServicesPerDay,
            energyToday: 0,
            energyTotal: campaign.TotalEnergy,
            energyQuota: campaign.Tuning.CampaignEnergyQuota,
            hasMaid: hasMaid,
            maidName: hasMaid ? maid.Profile.DisplayName : null,
            burden: hasMaid ? maid.Burden.Snapshot() : AxisTriple.Zero,
            burdenLimit: hasMaid ? maid.Burden.LimitSnapshot() : AxisTriple.Zero,
            controlStatus: ControlAuthorityStatus.Delegated,
            hasMonster: false,
            monsterName: null,
            demandAxis: BurdenAxis.Physical,
            isDemandKnown: false,
            satisfaction: 0,
            requiredSatisfaction: 0,
            phaseLabel: phaseLabel);
    }
}
