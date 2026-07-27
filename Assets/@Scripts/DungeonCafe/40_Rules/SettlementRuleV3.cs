using System;
using System.Collections.Generic;

/// <summary>접객이 어떤 방식으로 끝났는가. (v3 §3 상태 전이표)</summary>
public enum SettlementOutcomeKind
{
    /// <summary>비트 소진, 붕괴 ≤ 99.</summary>
    Normal = 0,

    /// <summary>심층 회수 구간에서 탈출.</summary>
    DepthEscape = 1,

    /// <summary>200 도달.</summary>
    TotalCollapse = 2,
}

/// <summary>v3 결산 내역. UI 결산창과 3장부 기입이 이 구조체를 그대로 쓴다.</summary>
public readonly struct SettlementV3Result
{
    public SettlementOutcomeKind Kind { get; }

    /// <summary>산입된 반응 점수 합. 심층 중 발생분은 호출부가 이미 제외한 값이다.</summary>
    public int ReactionScore { get; }

    public int Satisfaction { get; }
    public int RequiredSatisfaction { get; }
    public bool SatisfactionMet => Satisfaction >= RequiredSatisfaction;

    /// <summary>결산 기준 붕괴값 (요구축, 접객 종료 시점).</summary>
    public int EndCollapse { get; }

    /// <summary>미달 하향 전 배율.</summary>
    public float BaseMultiplier { get; }

    /// <summary>실제 적용 배율.</summary>
    public float AppliedMultiplier { get; }

    public bool WasDowngraded => AppliedMultiplier < BaseMultiplier;

    public string BandLabel { get; }

    /// <summary>획득 욕구. DesireLedger.Earn 에 그대로 넣는다.</summary>
    public int Energy { get; }

    public SettlementV3Result(
        SettlementOutcomeKind kind,
        int reactionScore,
        int satisfaction,
        int requiredSatisfaction,
        int endCollapse,
        float baseMultiplier,
        float appliedMultiplier,
        string bandLabel,
        int energy)
    {
        Kind = kind;
        ReactionScore = reactionScore;
        Satisfaction = satisfaction;
        RequiredSatisfaction = requiredSatisfaction;
        EndCollapse = endCollapse;
        BaseMultiplier = baseMultiplier;
        AppliedMultiplier = appliedMultiplier;
        BandLabel = bandLabel;
        Energy = energy;
    }

    public override string ToString()
        => $"[{Kind}] 반응 {ReactionScore} 만족 {Satisfaction}/{RequiredSatisfaction} "
           + $"붕괴 {EndCollapse} x{AppliedMultiplier:0.0} = 욕구 {Energy}";
}

/// <summary>
/// v3 결산. (v3 §7.2)
///
///   욕구 = 반응 점수 합 x 10 x 배율
///   배율: 0~49 x1.0 / 50~79 x1.5 / 80~99 x3.0 / 심층 탈출 x0.5 / 200 결산 0
///   만족도 미달 시 배율 사다리 1단 하향 (최저 = 심층 탈출 배율)
///
/// 99 와 100 사이의 낙차(x3.0 -> x0.5)가 이 게임의 핵심 도박이다.
/// 기존 ServiceSettlementCalculator 를 대체할 규칙이며, 이 단계에서는 나란히 존재한다.
/// 순수 계산 - 숙련 경험/후유증/장부 기입은 호출부가 결과를 받아 수행한다.
/// </summary>
public static class SettlementRuleV3
{
    public static SettlementV3Result Calculate(
        SettlementOutcomeKind kind,
        int reactionScore,
        int satisfaction,
        int requiredSatisfaction,
        int endCollapse,
        GuesthouseTuningV3 tuning)
    {
        reactionScore = Math.Max(0, reactionScore);

        if (kind == SettlementOutcomeKind.TotalCollapse)
        {
            return new SettlementV3Result(
                kind, reactionScore, satisfaction, requiredSatisfaction, endCollapse,
                baseMultiplier: 0f, appliedMultiplier: 0f, bandLabel: "완전 붕괴", energy: 0);
        }

        float baseMultiplier;
        string label;

        if (kind == SettlementOutcomeKind.DepthEscape || endCollapse >= tuning.ControlLossThreshold)
        {
            // 통상 종료로 들어와도 붕괴가 100 이상이면 심층 탈출로 취급한다 - 방어적 처리.
            baseMultiplier = tuning.DepthEscapeMultiplier;
            label = "심층 탈출";
        }
        else
        {
            CollapseMultiplierBand band = ResolveBand(tuning.SettlementBands, endCollapse);
            baseMultiplier = band.Multiplier;
            label = band.Label;
        }

        float appliedMultiplier = satisfaction >= requiredSatisfaction
            ? baseMultiplier
            : DowngradeOneStep(baseMultiplier, tuning);

        int energy = (int)Math.Floor(
            reactionScore * (long)tuning.EnergyPerReactionPoint * (double)appliedMultiplier);

        return new SettlementV3Result(
            kind, reactionScore, satisfaction, requiredSatisfaction, endCollapse,
            baseMultiplier, appliedMultiplier, label, energy);
    }

    private static CollapseMultiplierBand ResolveBand(
        IReadOnlyList<CollapseMultiplierBand> bands,
        int collapse)
    {
        CollapseMultiplierBand resolved = bands[0];

        for (int i = 0; i < bands.Count; i++)
        {
            if (collapse < bands[i].MinCollapse)
                break;

            resolved = bands[i];
        }

        return resolved;
    }

    /// <summary>
    /// 배율 사다리에서 한 단 아래로.
    /// 사다리 = [심층 탈출 배율] + 통상 밴드 배율들 (오름차순).
    /// 이미 최저 단이면 그대로.
    /// </summary>
    private static float DowngradeOneStep(float multiplier, GuesthouseTuningV3 tuning)
    {
        float best = tuning.DepthEscapeMultiplier;

        // 현재 배율보다 작은 것 중 가장 큰 값을 찾는다.
        IReadOnlyList<CollapseMultiplierBand> bands = tuning.SettlementBands;

        for (int i = 0; i < bands.Count; i++)
        {
            float candidate = bands[i].Multiplier;

            if (candidate < multiplier && candidate > best)
                best = candidate;
        }

        return multiplier <= tuning.DepthEscapeMultiplier
            ? tuning.DepthEscapeMultiplier
            : best;
    }
}
