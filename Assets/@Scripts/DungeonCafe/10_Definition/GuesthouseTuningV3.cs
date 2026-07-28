using System;
using System.Collections.Generic;

/// <summary>
/// guesthouse_design_v3.md 수치 전체를 담는 밸런스 상수 묶음. 표준 난이도 단일.
/// 1단계 필드는 전부 유지되며(기존 테스트 호환), 2단계 이후 상수가 추가되었다.
/// 캘린더/개체 풀 같은 '콘텐츠'는 GuesthouseV3ContentDB 쪽이고, 여기는 '숫자'만 둔다.
/// </summary>
public sealed class GuesthouseTuningV3
{
    // ---- 게이지 (§1, §3) ----
    public int ControlLossThreshold { get; }
    public int TotalCollapseThreshold { get; }

    // ---- 낮 부하 판정 (§2.3) ----
    public int DayAptitudeMitigationPerPoint { get; }
    public int DayMasteryMitigationPerLevel { get; }
    public int DayMinimumAppliedLoad { get; }

    // ---- 심층 (§4) ----
    public int DepthAptitudeDiePerPoint { get; }
    public int DepthModifierClampAbs { get; }
    public int DepthCollapseGainDivisor { get; }
    public int DepthMinBandWidth { get; }
    public int DepthBrandDieModifier { get; }
    public int DepthFullUnderstandingDieModifier { get; }
    public DepthBandLayout DepthStandardLayout { get; }

    // ---- 결산 (§7.2) ----
    public int EnergyPerReactionPoint { get; }
    public float DepthEscapeMultiplier { get; }
    public IReadOnlyList<CollapseMultiplierBand> SettlementBands => _settlementBands;
    private readonly CollapseMultiplierBand[] _settlementBands;

    // ---- 밤 (§6.1) ----
    public int NightCareReduction { get; }
    public int NightCareReductionUpgraded { get; }
    public int ManagedReleaseMinimumCollapse { get; } // 기본 80 ~ 99
    public int ManagedReleaseRetainPercent { get; }
    public int ManagedReleaseNightEnergy { get; }

    // ---- 방치 (§6.2) ----
    public int NeglectNaturalRecovery { get; }
    public int NeglectHoldPercent { get; }
    public int NeglectSelfReleasePercent { get; }
    public int NeglectSelfReleaseTargetCollapse { get; }
    public int NeglectSelfReleaseQuirkChancePercent { get; }
    public int NeglectQuirkRequestChancePercent { get; }
    public int NightIncidentDepthBeats { get; }

    // ---- 숙련 (§12.3) ----
    public IReadOnlyList<int> MasteryThresholds => _masteryThresholds;   // 누적 120/300/550
    private readonly int[] _masteryThresholds;
    public int ManagedReleaseMasteryExperience { get; }                  // 관리 붕괴 시 요구축 경험 보너스

    // ---- 관계 (§12.2) ----
    public IReadOnlyList<int> RelationStageThresholds => _relationStageThresholds; // 0/6/14/24
    private readonly int[] _relationStageThresholds;
    public int RelationPointsCare { get; }
    public int RelationPointsRelease { get; }
    public int RelationPointsAutoEvent { get; }
    public int HollowNightlyRelationPenalty { get; }

    // ---- 이해도 (§8.2, §13.4) ----
    public IReadOnlyList<int> UnderstandingTierThresholds => _understandingTierThresholds; // 2/5/9
    private readonly int[] _understandingTierThresholds;
    public int UnderstandingPerService { get; }
    public int UnderstandingPerPhoneCall { get; }
    public int UnderstandingPerDepthWitness { get; }
    public int UnderstandingPerAnalysis { get; }

    // ---- 후유증 (§9) ----
    public int BrandPermanentizeDays { get; }        // 각인 미해소 3일 -> 기벽 영구화
    public int TremorNeglectHealDays { get; }        // 떨림 방치 2일
    public int TremorCareCures { get; }              // 1
    public int BrandCareCures { get; }               // 2
    public int HollowCareCures { get; }              // 3

    // ---- 경제/가게 (§7.4, §8) ----
    public int BankruptcyLimit { get; }
    public IReadOnlyList<int> ShopLevelThresholds => _shopLevelThresholds; // 누적: 0/400/900/1600/2400/3300/4300
    private readonly int[] _shopLevelThresholds;
    public IReadOnlyList<int> AbilitySlotsByShopLevel => _abilitySlots;    // Lv1:1 Lv3:2 Lv7:3
    private readonly int[] _abilitySlots;
    public int NightManageCountShopLevel { get; }    // Lv6 -> 밤 2회
    public int CareUpgradeShopLevel { get; }         // Lv7 -> 안정 45
    public int AnalysisShopLevel { get; }            // Lv2 -> 밤 수첩 분석
    public int AnalysisTwiceShopLevel { get; }       // Lv7

    // ---- 엔딩 (§15) ----
    public int EndingSLifetime { get; }
    public int EndingALifetime { get; }

    public GuesthouseTuningV3(
        int controlLossThreshold, int totalCollapseThreshold,
        int dayAptitudeMitigationPerPoint, int dayMasteryMitigationPerLevel, int dayMinimumAppliedLoad,
        int depthAptitudeDiePerPoint, int depthModifierClampAbs, int depthCollapseGainDivisor,
        int depthMinBandWidth, int depthBrandDieModifier, int depthFullUnderstandingDieModifier,
        DepthBandLayout depthStandardLayout,
        int energyPerReactionPoint, float depthEscapeMultiplier,
        IReadOnlyList<CollapseMultiplierBand> settlementBands,
        int nightCareReduction, int nightCareReductionUpgraded,
        int managedReleaseMinimumCollapse, int managedReleaseRetainPercent, int managedReleaseNightEnergy,
        int neglectNaturalRecovery, int neglectHoldPercent, int neglectSelfReleasePercent,
        int neglectSelfReleaseTargetCollapse, int neglectSelfReleaseQuirkChancePercent,
        int neglectQuirkRequestChancePercent, int nightIncidentDepthBeats,
        IReadOnlyList<int> masteryThresholds, int managedReleaseMasteryExperience,
        IReadOnlyList<int> relationStageThresholds,
        int relationPointsCare, int relationPointsRelease, int relationPointsAutoEvent,
        int hollowNightlyRelationPenalty,
        IReadOnlyList<int> understandingTierThresholds,
        int understandingPerService, int understandingPerPhoneCall,
        int understandingPerDepthWitness, int understandingPerAnalysis,
        int brandPermanentizeDays, int tremorNeglectHealDays,
        int tremorCareCures, int brandCareCures, int hollowCareCures,
        int bankruptcyLimit,
        IReadOnlyList<int> shopLevelThresholds, IReadOnlyList<int> abilitySlotsByShopLevel,
        int nightManageCountShopLevel, int careUpgradeShopLevel,
        int analysisShopLevel, int analysisTwiceShopLevel,
        int endingSLifetime, int endingALifetime)
    {
        ControlLossThreshold = controlLossThreshold;
        TotalCollapseThreshold = totalCollapseThreshold;
        DayAptitudeMitigationPerPoint = dayAptitudeMitigationPerPoint;
        DayMasteryMitigationPerLevel = dayMasteryMitigationPerLevel;
        DayMinimumAppliedLoad = dayMinimumAppliedLoad;
        DepthAptitudeDiePerPoint = depthAptitudeDiePerPoint;
        DepthModifierClampAbs = depthModifierClampAbs;
        DepthCollapseGainDivisor = Math.Max(1, depthCollapseGainDivisor);
        DepthMinBandWidth = Math.Max(1, depthMinBandWidth);
        DepthBrandDieModifier = depthBrandDieModifier;
        DepthFullUnderstandingDieModifier = depthFullUnderstandingDieModifier;
        DepthStandardLayout = depthStandardLayout;
        EnergyPerReactionPoint = energyPerReactionPoint;
        DepthEscapeMultiplier = depthEscapeMultiplier;
        _settlementBands = SortedCopy(settlementBands);
        NightCareReduction = nightCareReduction;
        NightCareReductionUpgraded = nightCareReductionUpgraded;
        ManagedReleaseMinimumCollapse = managedReleaseMinimumCollapse;
        ManagedReleaseRetainPercent = managedReleaseRetainPercent;
        ManagedReleaseNightEnergy = managedReleaseNightEnergy;
        NeglectNaturalRecovery = neglectNaturalRecovery;
        NeglectHoldPercent = neglectHoldPercent;
        NeglectSelfReleasePercent = neglectSelfReleasePercent;
        NeglectSelfReleaseTargetCollapse = neglectSelfReleaseTargetCollapse;
        NeglectSelfReleaseQuirkChancePercent = neglectSelfReleaseQuirkChancePercent;
        NeglectQuirkRequestChancePercent = neglectQuirkRequestChancePercent;
        NightIncidentDepthBeats = nightIncidentDepthBeats;
        _masteryThresholds = Copy(masteryThresholds);
        ManagedReleaseMasteryExperience = managedReleaseMasteryExperience;
        _relationStageThresholds = Copy(relationStageThresholds);
        RelationPointsCare = relationPointsCare;
        RelationPointsRelease = relationPointsRelease;
        RelationPointsAutoEvent = relationPointsAutoEvent;
        HollowNightlyRelationPenalty = hollowNightlyRelationPenalty;
        _understandingTierThresholds = Copy(understandingTierThresholds);
        UnderstandingPerService = understandingPerService;
        UnderstandingPerPhoneCall = understandingPerPhoneCall;
        UnderstandingPerDepthWitness = understandingPerDepthWitness;
        UnderstandingPerAnalysis = understandingPerAnalysis;
        BrandPermanentizeDays = brandPermanentizeDays;
        TremorNeglectHealDays = tremorNeglectHealDays;
        TremorCareCures = tremorCareCures;
        BrandCareCures = brandCareCures;
        HollowCareCures = hollowCareCures;
        BankruptcyLimit = bankruptcyLimit;
        _shopLevelThresholds = Copy(shopLevelThresholds);
        _abilitySlots = Copy(abilitySlotsByShopLevel);
        NightManageCountShopLevel = nightManageCountShopLevel;
        CareUpgradeShopLevel = careUpgradeShopLevel;
        AnalysisShopLevel = analysisShopLevel;
        AnalysisTwiceShopLevel = analysisTwiceShopLevel;
        EndingSLifetime = endingSLifetime;
        EndingALifetime = endingALifetime;
    }

    /// <summary>guesthouse_design_v3.md 표준 난이도 값 그대로.</summary>
    public static GuesthouseTuningV3 CreateStandard()
    {
        return new GuesthouseTuningV3(
            controlLossThreshold: 100, totalCollapseThreshold: 200,                 // §3
            dayAptitudeMitigationPerPoint: 2, dayMasteryMitigationPerLevel: 1,     // §2.3
            dayMinimumAppliedLoad: 4,
            depthAptitudeDiePerPoint: 3, depthModifierClampAbs: 30,                // §4.2
            depthCollapseGainDivisor: 2, depthMinBandWidth: 4,                     // §4.3
            depthBrandDieModifier: 7, depthFullUnderstandingDieModifier: -5,       // §9, §8.2
            depthStandardLayout: DepthBandLayout.Standard,                         // §4.3
            energyPerReactionPoint: 10, depthEscapeMultiplier: 0.5f,               // §7.2
            settlementBands: new[]
            {
                new CollapseMultiplierBand(0, 1.0f, "안정"),
                new CollapseMultiplierBand(50, 1.5f, "동요"),
                new CollapseMultiplierBand(80, 3.0f, "한계 직전"),
            },
            nightCareReduction: 35, nightCareReductionUpgraded: 45,                // §6.1, §8
            managedReleaseMinimumCollapse: 80, managedReleaseRetainPercent: 50,
            managedReleaseNightEnergy: 40,
            neglectNaturalRecovery: 10, neglectHoldPercent: 60,                    // §6.2
            neglectSelfReleasePercent: 25, neglectSelfReleaseTargetCollapse: 60,
            neglectSelfReleaseQuirkChancePercent: 50, neglectQuirkRequestChancePercent: 25,
            nightIncidentDepthBeats: 2,
            masteryThresholds: new[] { 120, 300, 550 },                            // §12.3
            managedReleaseMasteryExperience: 20,
            relationStageThresholds: new[] { 0, 6, 14, 27 },                       // §12.2 (시뮬 보정: 24->27)
            relationPointsCare: 2, relationPointsRelease: 3, relationPointsAutoEvent: 1,
            hollowNightlyRelationPenalty: 1,                                       // §9
            understandingTierThresholds: new[] { 2, 5, 9 },                        // §13.4
            understandingPerService: 1, understandingPerPhoneCall: 1,
            understandingPerDepthWitness: 2, understandingPerAnalysis: 1,
            brandPermanentizeDays: 3, tremorNeglectHealDays: 2,                    // §9
            tremorCareCures: 1, brandCareCures: 2, hollowCareCures: 3,
            bankruptcyLimit: 3,                                                    // §7.4
            shopLevelThresholds: new[] { 0, 400, 900, 1600, 2400, 3300, 4300 },    // §8
            abilitySlotsByShopLevel: new[] { 1, 1, 2, 2, 2, 2, 3 },
            nightManageCountShopLevel: 6, careUpgradeShopLevel: 7,
            analysisShopLevel: 2, analysisTwiceShopLevel: 7,
            endingSLifetime: 6800, endingALifetime: 5000);                         // §15 (시뮬 실측 분포 기반 개정)
    }

    public int GetAbilitySlots(int shopLevel)
    {
        int index = Math.Clamp(shopLevel - 1, 0, _abilitySlots.Length - 1);
        return _abilitySlots[index];
    }

    public int GetNightManageCount(int shopLevel)
        => shopLevel >= NightManageCountShopLevel ? 2 : 1;

    public int GetCareReduction(int shopLevel)
        => shopLevel >= CareUpgradeShopLevel ? NightCareReductionUpgraded : NightCareReduction;

    public int GetAnalysisCount(int shopLevel)
    {
        if (shopLevel >= AnalysisTwiceShopLevel) return 2;
        return shopLevel >= AnalysisShopLevel ? 1 : 0;
    }

    private static int[] Copy(IReadOnlyList<int> source)
    {
        if (source == null) return Array.Empty<int>();
        int[] copied = new int[source.Count];
        for (int i = 0; i < source.Count; i++) copied[i] = source[i];
        return copied;
    }

    private static CollapseMultiplierBand[] SortedCopy(IReadOnlyList<CollapseMultiplierBand> source)
    {
        if (source == null || source.Count == 0)
            return new[] { new CollapseMultiplierBand(0, 1.0f, "안정") };
        CollapseMultiplierBand[] copied = new CollapseMultiplierBand[source.Count];
        for (int i = 0; i < source.Count; i++) copied[i] = source[i];
        Array.Sort(copied, static (a, b) => a.MinCollapse.CompareTo(b.MinCollapse));
        return copied;
    }
}
