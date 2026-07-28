using System.Collections.Generic;

/// <summary>
/// 가게 레벨 = 누적 획득 욕구 임계.
/// 레벨은 1부터.
/// </summary>
public static class ShopLevelRule
{
    public static int Resolve(int lifetimeDesire, DungeonCafeTuning tuning)
    {
        IReadOnlyList<int> t = tuning.ShopLevelThresholds;
        
        int level = 1;
        for (int i = 0; i < t.Count; i++)
            if (lifetimeDesire >= t[i]) level = i + 1;
        
        return level;
    }
}

/// <summary>
/// 관계 단계 판정.
/// 단계는 1~4.
/// </summary>
public static class RelationRule
{
    public static int ResolveStage(int points, DungeonCafeTuning tuning)
    {
        IReadOnlyList<int> t = tuning.RelationStageThresholds;
        
        int stage = 1;
        for (int i = 0; i < t.Count; i++)
            if (points >= t[i]) stage = i + 1;
        
        return stage;
    }
}

/// <summary>
/// 이해도 부여의 단일 창구.
/// </summary>
public static class UnderstandingRule
{
    public static void GrantServiceComplete(CampaignState campaign, string monsterId, MaidState maid)
    {
        int amount = campaign.Tuning.UnderstandingPerService;
        amount += QuirkEffectResolver.UnderstandingBonusOnSettle(campaign.Content, maid);
        campaign.Understanding.AddPoints(monsterId, amount);
    }

    public static bool GrantPhoneCall(CampaignState campaign, string monsterId)
    {
        if (!campaign.Understanding.MarkPhoneCalled(monsterId))
            return false;
        
        campaign.Understanding.AddPoints(monsterId, campaign.Tuning.UnderstandingPerPhoneCall);
        return true;
    }

    public static void GrantDepthWitness(CampaignState campaign, MonsterProfile monster, MaidState maid)
    {
        if (!campaign.Understanding.MarkDepthWitnessed(monster.MonsterId))
            return;

        int amount = campaign.Tuning.UnderstandingPerDepthWitness;
        if (QuirkEffectResolver.HasSpeciesBrandEcho(campaign.Content, maid, monster.Species))
            amount *= 2;
        
        campaign.Understanding.AddPoints(monster.MonsterId, amount);
    }

    public static void GrantAnalysis(CampaignState campaign, string monsterId) 
    {
        campaign.Understanding.AddPoints(monsterId, campaign.Tuning.UnderstandingPerAnalysis);
    }
}