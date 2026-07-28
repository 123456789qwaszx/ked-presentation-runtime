using System.Collections.Generic;

/// <summary>낮 옵션 1개의 표시 정보. 이해도에 따라 범위 노출이 달라진다. (§2.5)</summary>
public readonly struct OptionDisplay
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

    public OptionDisplay(int index, OptionIntensity intensity, BurdenAxis displayAxis,
        bool showsRange, int rangeMin, int rangeMax, bool upgradedReaction)
    {
        Index = index; Intensity = intensity; DisplayAxis = displayAxis;
        ShowsRange = showsRange; RangeMin = rangeMin; RangeMax = rangeMax;
        UpgradedReaction = upgradedReaction;
    }
}

/// <summary>낮 승인 요청.</summary>
public sealed class ApprovalRequest
{
    public ServiceSessionState Session { get; }
    public int BeatIndex { get; }
    public IReadOnlyList<OptionDisplay> Options { get; }
    /// <summary>이번 비트에 사용 가능한 낮 능력 id.</summary>
    public IReadOnlyList<string> AvailableAbilityIds { get; }
    public ApprovalRequest(ServiceSessionState session, int beatIndex,
        IReadOnlyList<OptionDisplay> options, IReadOnlyList<string> abilities)
    { Session = session; BeatIndex = beatIndex; Options = options; AvailableAbilityIds = abilities; }
}

public readonly struct ApprovalResponse
{
    public int OptionIndex { get; }
    public IReadOnlyList<string> UsedAbilityIds { get; }
    public ApprovalResponse(int optionIndex, IReadOnlyList<string> usedAbilityIds = null)
    { OptionIndex = optionIndex; UsedAbilityIds = usedAbilityIds; }
}

/// <summary>심층 굴림 직전 개입 요청.</summary>
public sealed class DepthInterventionRequest
{
    public ServiceSessionState Session { get; }
    public int DepthBeatIndex { get; }
    public DepthBandLayout Layout { get; }
    public bool LayoutRevealed { get; }
    /// <summary>징후 판독 등으로 공개된 최빈 구간. 미공개면 null.</summary>
    public DepthBand? PredictedBand { get; }
    public IReadOnlyList<string> AvailableAbilityIds { get; }
    public DepthInterventionRequest(ServiceSessionState session, int depthBeatIndex,
        DepthBandLayout layout, bool layoutRevealed, DepthBand? predicted,
        IReadOnlyList<string> abilities)
    {
        Session = session; DepthBeatIndex = depthBeatIndex; Layout = layout;
        LayoutRevealed = layoutRevealed; PredictedBand = predicted; AvailableAbilityIds = abilities;
    }
}

/// <summary>굴림 결과 제시 후 결정: 재굴림/구간 하향 능력 사용 (없으면 null).</summary>
public readonly struct DepthRollDecision
{
    public string RerollAbilityId { get; }
    public string DowngradeAbilityId { get; }
    public DepthRollDecision(string reroll, string downgrade)
    { RerollAbilityId = reroll; DowngradeAbilityId = downgrade; }
    public static DepthRollDecision None => default;
}

public sealed class NightPlanRequest
{
    public int DayNumber { get; }
    public int ManageCount { get; }
    public IReadOnlyList<MaidState> Maids { get; }
    public DungeonCafeTuning Tuning { get; }
    
    public IReadOnlyList<(string maidId, string quirkId)> QuirkRequests { get; }

    public NightPlanRequest(
        int day, 
        int manageCount,
        IReadOnlyList<MaidState> maids,
        DungeonCafeTuning tuning,
        IReadOnlyList<(string, string)> quirkRequests)
    {
        DayNumber = day; 
        ManageCount = manageCount;
        Maids = maids; 
        Tuning = tuning; 
        QuirkRequests = quirkRequests;
    }

    public bool CanRelease(MaidState maid)
    {
        // 떨림 후유증 중엔 케어만
        if (maid.FindAftereffect("se_tremor") != null)
            return false;
        
        maid.Gauge.HighestAxis(out int v);
        return v >= Tuning.ManagedReleaseMinimumCollapse && v < Tuning.ControlLossThreshold && !maid.IsLost;
    }
}

public readonly struct NightChoice
{
    public string MaidId { get; }
    public NightChoiceKind Kind { get; }
    public NightChoice(string maidId, NightChoiceKind kind) { MaidId = maidId; Kind = kind; }
}

public sealed class NightPrepRequest
{
    public IReadOnlyList<PlayerAbilityDefinition> Purchasable { get; }
    public IReadOnlyList<string> Owned { get; }
    public IReadOnlyList<string> Equipped { get; }
    public int SlotLimit { get; }
    public int HeldDesire { get; }

    public NightPrepRequest(
        IReadOnlyList<PlayerAbilityDefinition> purchasable, 
        IReadOnlyList<string> owned,
        IReadOnlyList<string> equipped,
        int slotLimit, 
        int held)
    {
        Purchasable = purchasable;
        Owned = owned; 
        Equipped = equipped;
        SlotLimit = slotLimit; 
        HeldDesire = held;
    }
}

public readonly struct NightPrepResponse
{
    public IReadOnlyList<string> PurchaseIds { get; }
    public IReadOnlyList<string> EquipIds { get; }

    public NightPrepResponse(IReadOnlyList<string> purchase, IReadOnlyList<string> equip)
    {
        PurchaseIds = purchase;
        EquipIds = equip;
    }
}
