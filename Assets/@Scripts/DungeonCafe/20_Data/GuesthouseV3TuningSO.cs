using UnityEngine;

/// <summary>
/// v3 튜닝 저작 SO. 인스펙터 항목 = design v3 §12 표 항목.
/// 노출하지 않은 세부값은 CreateStandard 기본을 따른다.
/// </summary>
[CreateAssetMenu(fileName = "GuesthouseV3Tuning", menuName = "Guesthouse/V3 Tuning")]
public sealed class GuesthouseV3TuningSO : ScriptableObject
{
    [Header("게이지 (§1/§3)")]
    [Min(1)] public int controlLossThreshold = 100;
    [Min(2)] public int totalCollapseThreshold = 200;

    [Header("낮 부하 (§2.3)")]
    [Min(0)] public int dayAptitudeMitigationPerPoint = 2;
    [Min(0)] public int dayMasteryMitigationPerLevel = 1;
    [Min(1)] public int dayMinimumAppliedLoad = 4;

    [Header("심층 (§4)")]
    [Min(0)] public int depthAptitudeDiePerPoint = 3;
    [Min(0)] public int depthModifierClampAbs = 30;
    [Min(1)] public int depthCollapseGainDivisor = 2;
    [Min(1)] public int depthMinBandWidth = 4;
    public int depthBrandDieModifier = 7;
    public int depthFullUnderstandingDieModifier = -5;
    [Min(1)] public int recoveryBandMax = 20;
    [Min(2)] public int riskyBandMax = 60;
    [Min(3)] public int fatalBandMax = 94;

    [Header("결산 (§7.2)")]
    [Min(1)] public int energyPerReactionPoint = 10;
    [Range(0f, 1f)] public float depthEscapeMultiplier = 0.5f;

    [Header("밤 (§6)")]
    [Min(0)] public int nightCareReduction = 35;
    [Min(0)] public int nightCareReductionUpgraded = 45;
    [Min(0)] public int managedReleaseMinimumCollapse = 80;
    [Range(0, 100)] public int managedReleaseRetainPercent = 50;
    [Min(0)] public int managedReleaseNightEnergy = 40;
    [Min(0)] public int neglectNaturalRecovery = 10;
    [Range(0, 100)] public int neglectHoldPercent = 60;
    [Range(0, 100)] public int neglectSelfReleasePercent = 25;
    [Min(0)] public int neglectSelfReleaseTargetCollapse = 60;
    [Range(0, 100)] public int neglectSelfReleaseQuirkChancePercent = 50;
    [Range(0, 100)] public int neglectQuirkRequestChancePercent = 25;
    [Min(1)] public int nightIncidentDepthBeats = 2;

    [Header("숙련/관계/이해도 (§12, §8.2)")]
    public int[] masteryThresholds = { 120, 300, 550 };
    [Min(0)] public int managedReleaseMasteryExperience = 20;
    public int[] relationStageThresholds = { 0, 6, 14, 27 };
    [Min(0)] public int relationPointsCare = 2;
    [Min(0)] public int relationPointsRelease = 3;
    public int[] understandingTierThresholds = { 2, 5, 9 };

    [Header("경제/가게 (§7.4/§8/§15)")]
    [Min(1)] public int bankruptcyLimit = 3;
    public int[] shopLevelThresholds = { 0, 400, 900, 1600, 2400, 3300, 4300 };
    public int[] abilitySlotsByShopLevel = { 1, 1, 2, 2, 2, 2, 3 };
    [Min(0)] public int endingSLifetime = 6800;
    [Min(0)] public int endingALifetime = 5000;

    public GuesthouseTuningV3 BuildTuning()
    {
        GuesthouseTuningV3 std = GuesthouseTuningV3.CreateStandard();
        return new GuesthouseTuningV3(
            controlLossThreshold, totalCollapseThreshold,
            dayAptitudeMitigationPerPoint, dayMasteryMitigationPerLevel, dayMinimumAppliedLoad,
            depthAptitudeDiePerPoint, depthModifierClampAbs, depthCollapseGainDivisor,
            depthMinBandWidth, depthBrandDieModifier, depthFullUnderstandingDieModifier,
            new DepthBandLayout(recoveryBandMax, riskyBandMax, fatalBandMax),
            energyPerReactionPoint, depthEscapeMultiplier,
            std.SettlementBands,
            nightCareReduction, nightCareReductionUpgraded,
            managedReleaseMinimumCollapse, managedReleaseRetainPercent, managedReleaseNightEnergy,
            neglectNaturalRecovery, neglectHoldPercent, neglectSelfReleasePercent,
            neglectSelfReleaseTargetCollapse, neglectSelfReleaseQuirkChancePercent,
            neglectQuirkRequestChancePercent, nightIncidentDepthBeats,
            masteryThresholds, managedReleaseMasteryExperience,
            relationStageThresholds, relationPointsCare, relationPointsRelease,
            std.RelationPointsAutoEvent, std.HollowNightlyRelationPenalty,
            understandingTierThresholds,
            std.UnderstandingPerService, std.UnderstandingPerPhoneCall,
            std.UnderstandingPerDepthWitness, std.UnderstandingPerAnalysis,
            std.BrandPermanentizeDays, std.TremorNeglectHealDays,
            std.TremorCareCures, std.BrandCareCures, std.HollowCareCures,
            bankruptcyLimit, shopLevelThresholds, abilitySlotsByShopLevel,
            std.NightManageCountShopLevel, std.CareUpgradeShopLevel,
            std.AnalysisShopLevel, std.AnalysisTwiceShopLevel,
            endingSLifetime, endingALifetime);
    }
}
