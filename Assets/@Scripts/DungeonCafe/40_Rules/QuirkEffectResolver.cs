using System.Collections.Generic;

/// <summary>
/// 메이드가 보유한 기벽 효과의 집계 창구. (§10)
/// 플로우는 기벽 id 목록을 직접 해석하지 않고 반드시 여기를 통해 묻는다.
/// </summary>
public static class QuirkEffectResolver
{
    private static IEnumerable<QuirkDefinition> Each(GuesthouseV3ContentDB content, MaidStateV3 maid)
    {
        IReadOnlyList<string> ids = maid.QuirkIds;
        for (int i = 0; i < ids.Count; i++)
        {
            QuirkDefinition def = content.GetQuirk(ids[i]);
            if (def != null) yield return def;
        }
    }

    /// <summary>심층 구간 레이아웃에 성향+기벽+능력 변형을 순서대로 적용.</summary>
    public static DepthBandLayout BuildDepthLayout(
        GuesthouseV3ContentDB content, MaidStateV3 maid,
        int abilityRecoveryShift, GuesthouseTuningV3 tuning)
    {
        DepthBandLayout layout = tuning.DepthStandardLayout;
        int w = tuning.DepthMinBandWidth;

        if (maid.Profile.TraitRecoveryShift != 0)
            layout = layout.ShiftRecoveryMax(maid.Profile.TraitRecoveryShift, w);
        if (maid.Profile.TraitRiskyShift != 0)
            layout = layout.ShiftRiskyMax(maid.Profile.TraitRiskyShift, w);

        foreach (QuirkDefinition q in Each(content, maid))
        {
            switch (q.EffectKind)
            {
                case QuirkEffectKind.RecoveryBandShift:
                    layout = layout.ShiftRecoveryMax(q.Magnitude, w); break;
                case QuirkEffectKind.RiskyFloorShift:
                    layout = layout.ShiftRecoveryMax(q.Magnitude, w); break; // 위험 하한 상승 = 회수 편입
                case QuirkEffectKind.HollowMark:
                    layout = layout.ShiftRecoveryMax(q.SecondaryMagnitude, w); break;
            }
        }

        if (abilityRecoveryShift != 0)
            layout = layout.ShiftRecoveryMax(abilityRecoveryShift, w);

        return layout;
    }

    /// <summary>심층 상태이상 보정 합 (아리에는 절반). (§9, §12.1)</summary>
    public static int StatusDieModifier(
        GuesthouseV3ContentDB content, MaidStateV3 maid, MonsterSpecies targetSpecies,
        GuesthouseTuningV3 tuning)
    {
        int sum = 0;

        IReadOnlyList<AftereffectInstance> effects = maid.Aftereffects;
        for (int i = 0; i < effects.Count; i++)
        {
            AftereffectDefinition def = effects[i].Definition;
            if (def.DepthDieModifier != 0
                && (def.TaggedSpecies == MonsterSpecies.None || def.TaggedSpecies == targetSpecies))
                sum += def.DepthDieModifier;
        }

        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.SpeciesBrandEcho
                && (q.TaggedSpecies == MonsterSpecies.None || q.TaggedSpecies == targetSpecies))
                sum += q.Magnitude;

        return sum * maid.Profile.StatusModifierPercent / 100;
    }

    public static bool HasSpeciesBrandEcho(GuesthouseV3ContentDB content, MaidStateV3 maid, MonsterSpecies species)
    {
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.SpeciesBrandEcho
                && (q.TaggedSpecies == MonsterSpecies.None || q.TaggedSpecies == species)) return true;
        return false;
    }

    /// <summary>지정 축 부하 2회 굴려 낮은 값 (임상적 거리).</summary>
    public static bool LoadRollTakeLowest(GuesthouseV3ContentDB content, MaidStateV3 maid, BurdenAxis axis)
    {
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.LoadRollTakeLowest && q.TaggedAxis == axis) return true;
        return false;
    }

    /// <summary>중 옵션 반응 강 승격 (태그 일치 시). (칼끝 예절/따라 부르기)</summary>
    public static bool MediumUpgrades(GuesthouseV3ContentDB content, MaidStateV3 maid, ServiceOptionV3 option)
    {
        if (option.Intensity != OptionIntensity.Medium) return false;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.MediumReactionUpgrade
                && !string.IsNullOrEmpty(q.OptionTag) && q.OptionTag == option.Tag)
                return true;
        return false;
    }

    public static int UnderstandingBonusOnSettle(GuesthouseV3ContentDB content, MaidStateV3 maid)
    {
        int sum = 0;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.UnderstandingOnSettle) sum += q.Magnitude;
        return sum;
    }

    public static int NeglectRecoveryBonus(GuesthouseV3ContentDB content, MaidStateV3 maid)
    {
        int sum = 0;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.NeglectRecoveryBonus) sum += q.Magnitude;
        return sum;
    }

    public static NeglectRule.NeglectChances NeglectChances(
        GuesthouseV3ContentDB content, MaidStateV3 maid, GuesthouseTuningV3 tuning)
    {
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.NeglectChancesOverride)
                return new NeglectRule.NeglectChances(q.Magnitude, q.SecondaryMagnitude);
        return NeglectRule.NeglectChances.From(tuning);
    }

    public static int ManagedRetainPercent(GuesthouseV3ContentDB content, MaidStateV3 maid, GuesthouseTuningV3 tuning)
    {
        int retain = tuning.ManagedReleaseRetainPercent;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.DependencyForming) retain -= q.Magnitude;
        return retain;
    }

    public static int ReleaseRelationBonus(GuesthouseV3ContentDB content, MaidStateV3 maid)
    {
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.DependencyForming) return 1;
        return 0;
    }

    public static int CareReductionDelta(GuesthouseV3ContentDB content, MaidStateV3 maid)
    {
        int sum = 0;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.HollowMark) sum -= q.Magnitude;
        return sum;
    }

    /// <summary>과몰입: 80~99 종료 시 (확률%, 추가부하, 반응+1). 없으면 chance 0.</summary>
    public static (int chancePercent, int extraLoad) OverImmersion(GuesthouseV3ContentDB content, MaidStateV3 maid)
    {
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.OverImmersion)
                return (q.SecondaryMagnitude, q.Magnitude);
        return (0, 0);
    }
}
