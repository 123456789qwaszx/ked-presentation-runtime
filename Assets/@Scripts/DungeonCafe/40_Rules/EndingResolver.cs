using System.Collections.Generic;

/// <summary>
/// 캠페인 엔딩 판정.
///
/// 판정은 위에서 아래로 순서대로 평가되며, 먼저 걸리는 규칙이 확정된다.
/// 규칙을 늘릴 때는 Resolve 의 순서만 조정하면 되고, 각 조건은 개별 메서드로 분리한다.
/// </summary>
public sealed class EndingResolver
{
    private readonly ProgressionTuning _tuning;
    private readonly IReadOnlyDictionary<MonsterSpecies, SpeciesProtocol> _protocolBySpecies;

    public EndingResolver(
        ProgressionTuning tuning,
        IReadOnlyDictionary<MonsterSpecies, SpeciesProtocol> protocolBySpecies)
    {
        _tuning = tuning;
        _protocolBySpecies = protocolBySpecies;
    }

    public CampaignEndingResult Resolve(CampaignState campaign)
    {
        if (TryResolveSpeciesCollapse(campaign, out CampaignEndingResult collapse))
            return collapse;

        if (TryResolveQuotaFailure(campaign, out CampaignEndingResult quotaFailure))
            return quotaFailure;

        if (TryResolveManagedGrowth(campaign, out CampaignEndingResult growth))
            return growth;

        return CampaignEndingResult.Create(
            endingKey: "Ending_Maintained",
            title: "영업 유지",
            nodeName: "Ending_Maintained",
            reason: "예약을 모두 소화했지만, 누구도 한 걸음 더 나아가지는 않았다.",
            isBadEnding: false);
    }

    /// <summary>
    /// 통제 상실 후 회수되지 못한 메이드가 있으면, 마지막으로 상대한 종족의 파멸로 수렴한다.
    /// </summary>
    private bool TryResolveSpeciesCollapse(CampaignState campaign, out CampaignEndingResult result)
    {
        MaidRuntimeState lost = null;

        for (int i = 0; i < campaign.Maids.Count; i++)
        {
            if (!campaign.Maids[i].IsLost)
                continue;

            lost = campaign.Maids[i];
            break;
        }

        if (lost == null)
        {
            result = null;
            return false;
        }

        MonsterSpecies species = FindLastIncidentSpecies(campaign, lost.MaidId);

        if (_protocolBySpecies.TryGetValue(species, out SpeciesProtocol protocol))
        {
            result = CampaignEndingResult.Create(
                endingKey: protocol.CollapseEndingKey,
                title: $"{protocol.DisplayName} 파멸",
                nodeName: protocol.CollapseEndingNodeName,
                reason: $"{lost.DisplayName}이(가) 통제 신호를 거부한 뒤 회수되지 못했다.",
                isBadEnding: true,
                collapseSpecies: species);

            return true;
        }

        result = CampaignEndingResult.Create(
            endingKey: "Ending_Collapse_Unknown",
            title: "회수 실패",
            nodeName: "Ending_Collapse_Unknown",
            reason: $"{lost.DisplayName}이(가) 격리실에서 돌아오지 않았다.",
            isBadEnding: true,
            collapseSpecies: species);

        return true;
    }

    private bool TryResolveQuotaFailure(CampaignState campaign, out CampaignEndingResult result)
    {
        if (campaign.TotalEnergy >= _tuning.CampaignEnergyQuota)
        {
            result = null;
            return false;
        }

        result = CampaignEndingResult.Create(
            endingKey: "Ending_QuotaFailed",
            title: "폐업",
            nodeName: "Ending_QuotaFailed",
            reason: $"확보한 에너지 {campaign.TotalEnergy} / 기준 {_tuning.CampaignEnergyQuota}",
            isBadEnding: true);

        return true;
    }

    /// <summary>
    /// 한계 근처까지 붕괴했지만 밤마다 회수되어 숙련으로 전환된 경우가 최상 결말이다.
    /// </summary>
    private bool TryResolveManagedGrowth(CampaignState campaign, out CampaignEndingResult result)
    {
        int totalLevels = campaign.CountTotalMasteryLevels();

        if (totalLevels < campaign.Maids.Count)
        {
            result = null;
            return false;
        }

        result = CampaignEndingResult.Create(
            endingKey: "Ending_ManagedGrowth",
            title: "관리된 성장",
            nodeName: "Ending_ManagedGrowth",
            reason: $"에너지 {campaign.TotalEnergy} / 숙련 레벨 합계 {totalLevels}",
            isBadEnding: false);

        return true;
    }

    private static MonsterSpecies FindLastIncidentSpecies(CampaignState campaign, string maidId)
    {
        MonsterSpecies species = MonsterSpecies.None;

        for (int d = 0; d < campaign.CompletedDays.Count; d++)
        {
            IReadOnlyList<ServiceSettlementResult> settlements = campaign.CompletedDays[d].Settlements;

            for (int s = 0; s < settlements.Count; s++)
            {
                if (!settlements[s].IsIncident)
                    continue;

                if (!string.Equals(settlements[s].MaidId, maidId, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                species = settlements[s].Species;
            }
        }

        return species;
    }
}
