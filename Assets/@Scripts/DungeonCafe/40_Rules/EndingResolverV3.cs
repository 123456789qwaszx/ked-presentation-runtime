/// <summary>엔딩 판정. 평가 순서: 전멸 → 폐업 → (완주) S → A → B. (§15)</summary>
public static class EndingResolverV3
{
    public static EndingKindV3 ResolveImmediate(CampaignStateV3 campaign)
    {
        if (campaign.AliveMaidCount == 0) return EndingKindV3.EmptyInn;
        if (campaign.BankruptcyCount >= campaign.Tuning.BankruptcyLimit) return EndingKindV3.Bankruptcy;
        return EndingKindV3.None;
    }

    public static EndingKindV3 ResolveCampaignEnd(CampaignStateV3 campaign)
    {
        EndingKindV3 immediate = ResolveImmediate(campaign);
        if (immediate != EndingKindV3.None) return immediate;

        bool allAlive = campaign.AliveMaidCount == campaign.Maids.Count;
        int lifetime = campaign.Ledger.Lifetime;

        if (allAlive && lifetime >= campaign.Tuning.EndingSLifetime && HasStage4(campaign))
            return EndingKindV3.FullHouseMorning;
        if (allAlive && lifetime >= campaign.Tuning.EndingALifetime)
            return EndingKindV3.NormalBusiness;
        return EndingKindV3.ClosingTime;
    }

    public static bool HasStage4(CampaignStateV3 campaign)
    {
        for (int i = 0; i < campaign.Maids.Count; i++)
            if (RelationRule.ResolveStage(campaign.Maids[i].RelationPoints, campaign.Tuning) >= 4)
                return true;
        return false;
    }
}
