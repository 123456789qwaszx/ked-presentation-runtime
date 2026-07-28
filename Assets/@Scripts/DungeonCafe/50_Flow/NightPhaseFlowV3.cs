using System;
using System.Collections.Generic;
using Yarn.Unity;

// (밤 진행)
// 상점과 장착 -> 선택 가능한 계획 제시 -> 한 명에게 회복 또는 붕괴 유도
// -> 선택받지 못한 메이드 방치 -> 후유증 경과 -> 심야 대화와 분석.
public sealed class NightPhaseFlowV3
{
    private readonly NightPrepFlowV3 _prepFlow;
    private readonly NightMaidFlowV3 _maidFlow;

    private readonly VnScreenBindings _screens;
    private readonly INodePlayerV3 _nodes;

    public NightPhaseFlowV3(
        NightPrepFlowV3 prepFlow,
        NightMaidFlowV3 maidFlow,
        VnScreenBindings screens,
        INodePlayerV3 nodes)
    {
        _prepFlow = prepFlow;
        _maidFlow = maidFlow;
        _screens = screens;
        _nodes = nodes;
    }

    public async YarnTask RunNightAsync(CampaignStateV3 campaign, DayStateV3 dayState)
    {
        // 상점에서 공용 능력을 구매 및 장착.
        campaign.Phase = CampaignPhaseV3.NightStart;
        await _prepFlow.RunAsync(campaign);

        campaign.Phase = CampaignPhaseV3.InNight;

        // 메이드 목록 제시. 후유증(배치불가) 상태여도 밤에는 나타남.
        List<MaidStateV3> present = campaign.GetPresent(dayState.DayNumber);

        // 오늘 밤 플레이어가 직접 손댈 수 있는 메이드 수 조건 체크
        int manageCount = campaign.Tuning.GetNightManageCount(campaign.ShopLevel);

        NightPlanRequestV3 request = new(
            dayState.DayNumber, 
            manageCount, 
            present,
            campaign.Tuning,
            campaign.DrainPendingQuirkRequests());

        IReadOnlyList<NightChoiceV3> choices = 
            await _screens.RequestNightPlanAsync(request);
        
        // 직접 처리(안정 + 관리 붕괴)한 인원. 나머지는 방치로 넘어간다.
        var handledIds = new HashSet<string>(StringComparer.Ordinal);

        int used = 0;

        for (int i = 0; i < choices.Count && used < manageCount; i++)
        {
            NightChoiceV3 choice = choices[i];
            MaidStateV3 maid = campaign.GetMaid(choice.MaidId);

            // if (maid.IsLost || handledIds.Contains(maid.MaidId))
            //     continue;

            if (choice.Kind == NightChoiceKind.Care)
            {
                await _maidFlow.RunCareAsync(campaign, maid);

                handledIds.Add(maid.MaidId);
                used++;
            }
            else if (choice.Kind == NightChoiceKind.ManagedRelease
                     && await _maidFlow.TryRunManagedReleaseAsync(campaign, maid))
            {
                handledIds.Add(maid.MaidId);
                used++;
            }
        }

        var dice = new CommittingDice(campaign, "neglect");

        for (int i = 0; i < present.Count; i++)
        {
            MaidStateV3 maid = present[i];
            if (!handledIds.Contains(maid.MaidId))
                await _maidFlow.RunNeglectAsync(campaign, maid, dayState, dice);
        }

        // 심야 대화 후, 오늘 손님 중 가장 모르는 쪽부터 분석 및 수첩 기록.
        await _nodes.PlayNodeAsync($"Night_Talk_{dayState.DayNumber}");

        int analysisCount = campaign.Tuning.GetAnalysisCount(campaign.ShopLevel);

        for (int i = 0; i < analysisCount && i < dayState.Bookings.Count; i++)
        {
            string monsterId = campaign.Understanding.FindLeastUnderstood(dayState.Bookings);
            UnderstandingRule.GrantAnalysis(campaign, monsterId);
        }

        campaign.Phase = CampaignPhaseV3.DayEnd;
    }
}