using System.Collections.Generic;

/// <summary>
/// 메이드가 보유한 기벽 효과의 집계 창구.
/// 플로우는 기벽 id 목록을 직접 해석하지 않고 반드시 여기를 통해 묻는다.
/// </summary>
public static class QuirkEffectResolver
{
    private static IEnumerable<QuirkDefinition> Each(
        DungeonCafeContentDB content, 
        MaidState maid)
    {
        IReadOnlyList<string> ids = maid.QuirkIds;
        for (int i = 0; i < ids.Count; i++)
        {
            QuirkDefinition def = content.GetQuirk(ids[i]);
            if (def != null) yield return def;
        }
    }

    // 심층 구간 레이아웃에 성향+기벽+능력 변형을 순서대로 적용.
    public static DepthBandLayout BuildDepthLayout(
        DungeonCafeContentDB content,
        MaidState maid,
        int abilityRecoveryShift, 
        DungeonCafeTuning tuning)
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

    // 심층 상태이상 보정 합
    public static int StatusDieModifier(
        DungeonCafeContentDB content, 
        MaidState maid, 
        MonsterSpecies targetSpecies,
        DungeonCafeTuning tuning)
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

    public static bool HasSpeciesBrandEcho(
        DungeonCafeContentDB content, 
        MaidState maid, 
        MonsterSpecies species)
    {
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.SpeciesBrandEcho
                && (q.TaggedSpecies == MonsterSpecies.None || q.TaggedSpecies == species)) return true;
        return false;
    }

    // 지정 축 부하 2회 굴려 낮은 값 (임상적 거리).
    public static bool LoadRollTakeLowest(
        DungeonCafeContentDB content,
        MaidState maid, 
        BurdenAxis axis)
    {
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.LoadRollTakeLowest && q.TaggedAxis == axis) return true;
        return false;
    }

    // 중 옵션 반응 강 승격 (태그 일치 시).
    public static bool MediumUpgrades(
        DungeonCafeContentDB content,
        MaidState maid, 
        ServiceOption option)
    {
        if (option.Intensity != OptionIntensity.Medium) return false;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.MediumReactionUpgrade
                && !string.IsNullOrEmpty(q.OptionTag) && q.OptionTag == option.Tag)
                return true;
        return false;
    }

    public static int UnderstandingBonusOnSettle(
        DungeonCafeContentDB content,
        MaidState maid)
    {
        int sum = 0;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.UnderstandingOnSettle) sum += q.Magnitude;
        return sum;
    }

    public static int NeglectRecoveryBonus(
        DungeonCafeContentDB content,
        MaidState maid)
    {
        int sum = 0;
        foreach (QuirkDefinition q in Each(content, maid))
            if (q.EffectKind == QuirkEffectKind.NeglectRecoveryBonus) sum += q.Magnitude;
        return sum;
    }

    public static NeglectRule.NeglectChances NeglectChances(
        DungeonCafeContentDB content, 
        MaidState maid, 
        DungeonCafeTuning tuning)
    {
        foreach (QuirkDefinition q in Each(content, maid))
        {
            if (q.EffectKind == QuirkEffectKind.NeglectChancesOverride)
                return new NeglectRule.NeglectChances(
                    q.Magnitude,
                    q.SecondaryMagnitude);
        }
        
        return NeglectRule.NeglectChances.From(tuning);
    }

    public static int ReleaseRelationBonus(
        DungeonCafeContentDB content, 
        MaidState maid)
    {
        foreach (QuirkDefinition q in Each(content, maid))
        {
            if (q.EffectKind == QuirkEffectKind.DependencyForming)
                return 1;
        }
        
        return 0;
    }

    public static int CareReductionDelta(DungeonCafeContentDB content, MaidState maid)
    {
        int sum = 0;
        foreach (QuirkDefinition q in Each(content, maid))
        {
            if (q.EffectKind == QuirkEffectKind.HollowMark) sum -= q.Magnitude;
        }
        
        return sum;
    }

    // 과몰입: 80~99 종료 시 (확률%, 추가부하, 반응+1). 없으면 chance 0.
    public static (int chancePercent, int extraLoad) OverImmersion(DungeonCafeContentDB content, MaidState maid)
    {
        foreach (QuirkDefinition q in Each(content, maid))
        {
            if (q.EffectKind == QuirkEffectKind.OverImmersion)
                return (q.SecondaryMagnitude, q.Magnitude);
        }
        
        return (0, 0);
    }
}