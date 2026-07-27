using System;
using System.Collections.Generic;

/// <summary>가게 레벨 = 누적 획득 욕구 임계 (§8). 레벨은 1부터.</summary>
public static class ShopLevelRule
{
    public static int Resolve(int lifetimeDesire, GuesthouseTuningV3 tuning)
    {
        IReadOnlyList<int> t = tuning.ShopLevelThresholds;
        int level = 1;
        for (int i = 0; i < t.Count; i++)
            if (lifetimeDesire >= t[i]) level = i + 1;
        return level;
    }
}

/// <summary>관계 단계 판정 (§12.2). 단계는 1~4.</summary>
public static class RelationRule
{
    public static int ResolveStage(int points, GuesthouseTuningV3 tuning)
    {
        IReadOnlyList<int> t = tuning.RelationStageThresholds;
        int stage = 1;
        for (int i = 0; i < t.Count; i++)
            if (points >= t[i]) stage = i + 1;
        return stage;
    }
}

/// <summary>이해도 부여의 단일 창구 (§8.2). 아리에 [기록 습관] 가산 포함.</summary>
public static class UnderstandingRule
{
    public static void GrantServiceComplete(
        CampaignStateV3 campaign, string monsterId, MaidStateV3 maid)
    {
        int amount = campaign.Tuning.UnderstandingPerService;
        amount += QuirkEffectResolver.UnderstandingBonusOnSettle(campaign.Content, maid);
        campaign.Understanding.AddPoints(monsterId, amount);
    }

    public static void GrantPhoneCall(CampaignStateV3 campaign, string monsterId)
    {
        if (campaign.Understanding.MarkPhoneCalled(monsterId))
            campaign.Understanding.AddPoints(monsterId, campaign.Tuning.UnderstandingPerPhoneCall);
    }

    /// <summary>심층 목격: 첫 진입 시 +2, [각인 잔향] 태그 종족은 x2. (§4.4, §10.2)</summary>
    public static void GrantDepthWitness(
        CampaignStateV3 campaign, MonsterProfileV3 monster, MaidStateV3 maid)
    {
        if (!campaign.Understanding.MarkDepthWitnessed(monster.MonsterId)) return;

        int amount = campaign.Tuning.UnderstandingPerDepthWitness;
        if (QuirkEffectResolver.HasSpeciesBrandEcho(campaign.Content, maid, monster.Species))
            amount *= 2;
        campaign.Understanding.AddPoints(monster.MonsterId, amount);
    }

    public static void GrantAnalysis(CampaignStateV3 campaign, string monsterId)
        => campaign.Understanding.AddPoints(monsterId, campaign.Tuning.UnderstandingPerAnalysis);
}
