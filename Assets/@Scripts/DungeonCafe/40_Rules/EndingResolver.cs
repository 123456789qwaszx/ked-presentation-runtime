// (엔딩 판정)
// 평가 순서: 전멸 -> 폐업 -> 완주
public static class EndingResolver
{
    public static EndingKind ResolveImmediate(CampaignState campaign)
    {
        if (campaign.AliveMaidCount == 0)
            return EndingKind.EmptyInn;
        
        if (campaign.BankruptcyCount >= campaign.Tuning.BankruptcyLimit)
            return EndingKind.Bankruptcy;
        
        return EndingKind.None;
    }

    public static EndingKind ResolveCampaignEnd(CampaignState campaign)
    {
        EndingKind immediate = ResolveImmediate(campaign);
        if (immediate != EndingKind.None) return immediate;

        bool allAlive = campaign.AliveMaidCount == campaign.Maids.Count;
        int lifetime = campaign.Ledger.Lifetime;

        if (allAlive && lifetime >= campaign.Tuning.EndingSLifetime && HasStage4(campaign))
            return EndingKind.FullHouseMorning;
        if (allAlive && lifetime >= campaign.Tuning.EndingALifetime)
            return EndingKind.NormalBusiness;
        return EndingKind.ClosingTime;
    }

    public static bool HasStage4(CampaignState campaign)
    {
        for (int i = 0; i < campaign.Maids.Count; i++)
            if (RelationRule.ResolveStage(campaign.Maids[i].RelationPoints, campaign.Tuning) >= 4)
                return true;
        return false;
    }
}
