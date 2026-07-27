using System;

/// <summary>능력 해금 4-튜플 판정과 구매. (§11) 사용 가능 여부는 PlayerAbilityState.CanUse.</summary>
public static class AbilityRules
{
    /// <summary>지식·관계·가게레벨 조건 충족 여부 (비용 제외).</summary>
    public static bool MeetsConditions(CampaignStateV3 campaign, PlayerAbilityDefinition def)
    {
        if (campaign.ShopLevel < def.ShopLevelRequired) return false;
        if (!MeetsKnowledge(campaign, def)) return false;

        if (def.OwnerMaidId != null)
        {
            MaidStateV3 maid = campaign.GetMaid(def.OwnerMaidId);
            if (maid == null) return false;
            int stage = RelationRule.ResolveStage(maid.RelationPoints, campaign.Tuning);
            if (stage < def.RelationStageRequired) return false;
        }

        return true;
    }

    private static bool MeetsKnowledge(CampaignStateV3 campaign, PlayerAbilityDefinition def)
    {
        UnderstandingState u = campaign.Understanding;
        GuesthouseV3ContentDB c = campaign.Content;
        GuesthouseTuningV3 t = campaign.Tuning;

        return def.KnowledgeGate switch
        {
            KnowledgeGateKind.None => true,
            KnowledgeGateKind.AnyPartialCount
                => u.CountAtTier(c, t, UnderstandingTier.Partial) >= def.KnowledgeCount,
            KnowledgeGateKind.AnyCompleteCount
                => u.CountAtTier(c, t, UnderstandingTier.Complete) >= def.KnowledgeCount,
            KnowledgeGateKind.DepthWitnessCount
                => u.DepthWitnessTotal >= def.KnowledgeCount,
            KnowledgeGateKind.TypeServiceCount
                => campaign.ServiceCountByType(def.KnowledgeType) >= def.KnowledgeCount,
            KnowledgeGateKind.TypeCompleteCount
                => u.CountTypeAtTier(c, t, def.KnowledgeType, UnderstandingTier.Complete) >= def.KnowledgeCount,
            KnowledgeGateKind.TypeWitnessCount
                => campaign.WitnessCountByType(def.KnowledgeType) >= def.KnowledgeCount,
            _ => false,
        };
    }

    /// <summary>구매: 조건 충족 + 보유 욕구 지불. 전용 능력은 비용 0 — 밤 이벤트로 습득. (§11.3)</summary>
    public static bool TryPurchase(CampaignStateV3 campaign, PlayerAbilityDefinition def)
    {
        if (campaign.Abilities.Owns(def.Id)) return false;
        if (!MeetsConditions(campaign, def)) return false;
        if (!campaign.Ledger.TrySpend(def.DesireCost)) return false;
        campaign.Abilities.Grant(def.Id);
        return true;
    }
}
