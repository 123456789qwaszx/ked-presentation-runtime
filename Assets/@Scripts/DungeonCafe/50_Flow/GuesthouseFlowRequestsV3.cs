using System.Collections.Generic;

/// <summary>낮 옵션 1개의 표시 정보. 이해도에 따라 범위 노출이 달라진다. (§2.5)</summary>
public readonly struct OptionDisplayV3
{
    public int Index { get; }
    public OptionIntensity Intensity { get; }
    /// <summary>표시용 축. 이면잉크 위장이 반영된 값. (§13.2)</summary>
    public BurdenAxis DisplayAxis { get; }
    public bool ShowsRange { get; }
    public int RangeMin { get; }
    public int RangeMax { get; }
    /// <summary>강 등급으로 산정될 옵션인지 (특이 규칙/기벽 반영 예고).</summary>
    public bool UpgradedReaction { get; }

    public OptionDisplayV3(int index, OptionIntensity intensity, BurdenAxis displayAxis,
        bool showsRange, int rangeMin, int rangeMax, bool upgradedReaction)
    {
        Index = index; Intensity = intensity; DisplayAxis = displayAxis;
        ShowsRange = showsRange; RangeMin = rangeMin; RangeMax = rangeMax;
        UpgradedReaction = upgradedReaction;
    }
}

/// <summary>낮 승인 요청.</summary>
public sealed class ApprovalRequestV3
{
    public ServiceSessionStateV3 Session { get; }
    public int BeatIndex { get; }
    public IReadOnlyList<OptionDisplayV3> Options { get; }
    /// <summary>이번 비트에 사용 가능한 낮 능력 id.</summary>
    public IReadOnlyList<string> AvailableAbilityIds { get; }
    public ApprovalRequestV3(ServiceSessionStateV3 session, int beatIndex,
        IReadOnlyList<OptionDisplayV3> options, IReadOnlyList<string> abilities)
    { Session = session; BeatIndex = beatIndex; Options = options; AvailableAbilityIds = abilities; }
}

public readonly struct ApprovalResponseV3
{
    public int OptionIndex { get; }
    public IReadOnlyList<string> UsedAbilityIds { get; }
    public ApprovalResponseV3(int optionIndex, IReadOnlyList<string> usedAbilityIds = null)
    { OptionIndex = optionIndex; UsedAbilityIds = usedAbilityIds; }
}

/// <summary>심층 굴림 직전 개입 요청.</summary>
public sealed class DepthInterventionRequestV3
{
    public ServiceSessionStateV3 Session { get; }
    public int DepthBeatIndex { get; }
    public DepthBandLayout Layout { get; }
    public bool LayoutRevealed { get; }
    /// <summary>징후 판독 등으로 공개된 최빈 구간. 미공개면 null.</summary>
    public DepthBand? PredictedBand { get; }
    public IReadOnlyList<string> AvailableAbilityIds { get; }
    public DepthInterventionRequestV3(ServiceSessionStateV3 session, int depthBeatIndex,
        DepthBandLayout layout, bool layoutRevealed, DepthBand? predicted,
        IReadOnlyList<string> abilities)
    {
        Session = session; DepthBeatIndex = depthBeatIndex; Layout = layout;
        LayoutRevealed = layoutRevealed; PredictedBand = predicted; AvailableAbilityIds = abilities;
    }
}

/// <summary>굴림 결과 제시 후 결정: 재굴림/구간 하향 능력 사용 (없으면 null).</summary>
public readonly struct DepthRollDecisionV3
{
    public string RerollAbilityId { get; }
    public string DowngradeAbilityId { get; }
    public DepthRollDecisionV3(string reroll, string downgrade)
    { RerollAbilityId = reroll; DowngradeAbilityId = downgrade; }
    public static DepthRollDecisionV3 None => default;
}

/// <summary>밤 계획 요청. manageCount 명까지 (maidId, 처리) 지정, 나머지는 방치. (§5.1)</summary>
public sealed class NightPlanRequestV3
{
    public int DayNumber { get; }
    public int ManageCount { get; }
    public IReadOnlyList<MaidStateV3> Maids { get; }
    public GuesthouseTuningV3 Tuning { get; }
    /// <summary>먼저 요구하는 이벤트 예약 (maidId, quirkId). (§6.2)</summary>
    public IReadOnlyList<(string maidId, string quirkId)> QuirkRequests { get; }
    public NightPlanRequestV3(int day, int manageCount, IReadOnlyList<MaidStateV3> maids,
        GuesthouseTuningV3 tuning, IReadOnlyList<(string, string)> quirkRequests)
    { DayNumber = day; ManageCount = manageCount; Maids = maids; Tuning = tuning; QuirkRequests = quirkRequests; }

    public bool CanRelease(MaidStateV3 maid)
    {
        maid.Gauge.HighestAxis(out int v);
        return v >= Tuning.ManagedReleaseMinimumCollapse && v < Tuning.ControlLossThreshold && !maid.IsLost;
    }
}

public readonly struct NightChoiceV3
{
    public string MaidId { get; }
    public NightChoiceKind Kind { get; }
    public NightChoiceV3(string maidId, NightChoiceKind kind) { MaidId = maidId; Kind = kind; }
}

/// <summary>밤 시작 상점: 구매/장착 결정.</summary>
public sealed class NightPrepRequestV3
{
    public IReadOnlyList<PlayerAbilityDefinition> Purchasable { get; }
    public IReadOnlyList<string> Owned { get; }
    public IReadOnlyList<string> Equipped { get; }
    public int SlotLimit { get; }
    public int HeldDesire { get; }
    public NightPrepRequestV3(IReadOnlyList<PlayerAbilityDefinition> purchasable,
        IReadOnlyList<string> owned, IReadOnlyList<string> equipped, int slotLimit, int held)
    { Purchasable = purchasable; Owned = owned; Equipped = equipped; SlotLimit = slotLimit; HeldDesire = held; }
}

public readonly struct NightPrepResponseV3
{
    public IReadOnlyList<string> PurchaseIds { get; }
    public IReadOnlyList<string> EquipIds { get; }
    public NightPrepResponseV3(IReadOnlyList<string> purchase, IReadOnlyList<string> equip)
    { PurchaseIds = purchase; EquipIds = equip; }
}
