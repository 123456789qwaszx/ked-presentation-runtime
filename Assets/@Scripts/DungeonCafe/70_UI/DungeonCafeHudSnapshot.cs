/// <summary>
/// 상시 표시 UI 가 한 번에 필요로 하는 값의 스냅숏. (v3)
///
/// 오버레이가 캠페인/세션 상태 객체를 직접 붙들면 표현 계층이 진행 상태에 결합된다.
/// 그래서 매 갱신마다 값 복사본을 만들어 넘기고, UI 는 이 구조체 외에는 아무것도 모른다.
/// Unity 무의존 - 헤드리스에서도 그대로 만들어진다.
///
/// v2 와의 차이: 붕괴 게이지는 0~200 단일 스케일이다. 한계(limit) 개념은 없다.
/// 80~99 = 정답 구간(위험 착지), 100+ = 통제 상실(심층), 200 = 완전 붕괴.
/// </summary>
public readonly struct DungeonCafeHudSnapshot
{
    public readonly int DayNumber;
    public readonly int DayCount;
    public readonly int SlotIndex;      // 1-base 표시용. 세션 밖이면 0.
    public readonly int SlotCount;

    public readonly int EnergyToday;
    public readonly int EnergyHeld;
    public readonly int EnergyLifetime;
    public readonly int EnergyQuota;    // 오늘 할당
    public readonly int ShopLevel;

    public readonly bool HasMaid;
    public readonly string MaidName;
    public readonly AxisTriple Gauge;
    public readonly int ControlLossThreshold;   // 100
    public readonly int TotalCollapseThreshold; // 200
    public readonly int DangerBandFloor;        // 80

    public readonly bool HasMonster;
    public readonly string MonsterName;
    public readonly BurdenAxis DemandAxis;
    public readonly bool IsDemandKnown;         // 이해도 일부 파악 이상
    public readonly int Satisfaction;
    public readonly int RequiredSatisfaction;

    /// <summary>지금 화면에서 진행 중인 구간. 오버레이 머리말에 그대로 쓴다.</summary>
    public readonly string PhaseLabel;

    private DungeonCafeHudSnapshot(
        int dayNumber, int dayCount, int slotIndex, int slotCount,
        int energyToday, int energyHeld, int energyLifetime, int energyQuota, int shopLevel,
        bool hasMaid, string maidName, AxisTriple gauge,
        int controlLossThreshold, int totalCollapseThreshold, int dangerBandFloor,
        bool hasMonster, string monsterName, BurdenAxis demandAxis, bool isDemandKnown,
        int satisfaction, int requiredSatisfaction, string phaseLabel)
    {
        DayNumber = dayNumber; DayCount = dayCount; SlotIndex = slotIndex; SlotCount = slotCount;
        EnergyToday = energyToday; EnergyHeld = energyHeld; EnergyLifetime = energyLifetime;
        EnergyQuota = energyQuota; ShopLevel = shopLevel;
        HasMaid = hasMaid; MaidName = maidName; Gauge = gauge;
        ControlLossThreshold = controlLossThreshold;
        TotalCollapseThreshold = totalCollapseThreshold;
        DangerBandFloor = dangerBandFloor;
        HasMonster = hasMonster; MonsterName = monsterName; DemandAxis = demandAxis;
        IsDemandKnown = isDemandKnown;
        Satisfaction = satisfaction; RequiredSatisfaction = requiredSatisfaction;
        PhaseLabel = phaseLabel;
    }

    /// <summary>접객 밖(게시판/배정/밤/리포트) 스냅숏.</summary>
    public static DungeonCafeHudSnapshot FromCampaign(CampaignState campaign, string phaseLabel)
    {
        CampaignDayPlan plan = campaign.Content.GetDayPlan(campaign.CurrentDayNumber);

        return new DungeonCafeHudSnapshot(
            dayNumber: campaign.CurrentDayNumber,
            dayCount: campaign.Content.CampaignDayCount,
            slotIndex: 0,
            slotCount: plan?.ServiceSlots ?? 0,
            energyToday: campaign.Ledger.Today,
            energyHeld: campaign.Ledger.Held,
            energyLifetime: campaign.Ledger.Lifetime,
            energyQuota: plan?.Quota ?? 0,
            shopLevel: campaign.ShopLevel,
            hasMaid: false, maidName: string.Empty, gauge: AxisTriple.Zero,
            controlLossThreshold: campaign.Tuning.ControlLossThreshold,
            totalCollapseThreshold: campaign.Tuning.TotalCollapseThreshold,
            dangerBandFloor: campaign.Tuning.ManagedReleaseMinimumCollapse,
            hasMonster: false, monsterName: string.Empty,
            demandAxis: BurdenAxis.Physical, isDemandKnown: false,
            satisfaction: 0, requiredSatisfaction: 0,
            phaseLabel: phaseLabel);
    }

    /// <summary>접객 중 스냅숏. 세션의 메이드/개체/만족도를 함께 싣는다.</summary>
    public static DungeonCafeHudSnapshot FromSession(
        CampaignState campaign, ServiceSessionState session, int slotIndex, string phaseLabel)
    {
        DungeonCafeHudSnapshot baseSnapshot = FromCampaign(campaign, phaseLabel);
        UnderstandingTier tier = campaign.Understanding.GetTier(session.Monster.MonsterId, campaign.Tuning);

        return new DungeonCafeHudSnapshot(
            dayNumber: baseSnapshot.DayNumber,
            dayCount: baseSnapshot.DayCount,
            slotIndex: slotIndex,
            slotCount: baseSnapshot.SlotCount,
            energyToday: baseSnapshot.EnergyToday,
            energyHeld: baseSnapshot.EnergyHeld,
            energyLifetime: baseSnapshot.EnergyLifetime,
            energyQuota: baseSnapshot.EnergyQuota,
            shopLevel: baseSnapshot.ShopLevel,
            hasMaid: true,
            maidName: session.Maid.DisplayName,
            gauge: session.Maid.Gauge.Snapshot(),
            controlLossThreshold: baseSnapshot.ControlLossThreshold,
            totalCollapseThreshold: baseSnapshot.TotalCollapseThreshold,
            dangerBandFloor: baseSnapshot.DangerBandFloor,
            hasMonster: true,
            monsterName: session.Monster.DisplayName,
            demandAxis: session.Monster.DemandAxis,
            isDemandKnown: tier >= UnderstandingTier.Partial,
            satisfaction: session.Satisfaction,
            requiredSatisfaction: session.Monster.RequiredSatisfaction,
            phaseLabel: phaseLabel);
    }

    /// <summary>요구축 기준 붕괴 구간 문구. 절벽 전이를 오버레이 머리에 노출한다.</summary>
    public string ControlLabel
    {
        get
        {
            if (!HasMaid) return string.Empty;

            int v = HasMonster ? Gauge[DemandAxis] : Highest();

            if (v >= TotalCollapseThreshold) return "완전 붕괴";
            if (v >= ControlLossThreshold) return "관리자 통제 신호가 거부되었습니다";
            if (v >= DangerBandFloor) return "위험 착지 구간 (결산 x3.0)";
            return "행동 승인권 위임 중";
        }
    }

    private int Highest()
    {
        int p = Gauge.Physical, m = Gauge.Mental, e = Gauge.Empathic;
        int h = p > m ? p : m;
        return h > e ? h : e;
    }
}
