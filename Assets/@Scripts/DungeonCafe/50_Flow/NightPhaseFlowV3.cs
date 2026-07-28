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

        // 메이드 목록 제시. 배정 불가 상태여도 밤에는 나타남.
        List<MaidStateV3> present = campaign.GetPresent(dayState.DayNumber);

        int manageCount = campaign.Tuning.GetNightManageCount(campaign.ShopLevel);

        NightPlanRequestV3 request = new(
            dayState.DayNumber, manageCount, present,
            campaign.Tuning, campaign.DrainPendingQuirkRequests());

        IReadOnlyList<NightChoiceV3> choices = await _screens.RequestNightPlanAsync(request);

        // 직접 처리한 인원. 안정만 후유증 진행과 관계 판정에 따로 쓰이므로 둘로 나눈다.
        var caredIds = new HashSet<string>(StringComparer.Ordinal);
        var handledIds = new HashSet<string>(StringComparer.Ordinal);

        // 지정 인원 처리. 관리 붕괴는 조건을 미만일 시 제시되지 않음.
        int used = 0;

        for (int i = 0; i < choices.Count && used < manageCount; i++)
        {
            NightChoiceV3 choice = choices[i];
            MaidStateV3 maid = campaign.GetMaid(choice.MaidId);

            if (maid == null || maid.IsLost || handledIds.Contains(maid.MaidId))
                continue;

            if (choice.Kind == NightChoiceKind.Care)
            {
                await _maidFlow.RunCareAsync(campaign, maid);

                caredIds.Add(maid.MaidId);
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

        // 남은 인원 방치. 메이드 순서가 곧 방치 판정의 난수 소비 순서다.
        var dice = new CommittingDice(campaign, "neglect");

        for (int i = 0; i < present.Count; i++)
        {
            MaidStateV3 maid = present[i];

            if (!handledIds.Contains(maid.MaidId))
                await _maidFlow.RunNeglectAsync(campaign, maid, dayState, dice);

            // 관리 붕괴로 처리했어도 안정을 준 것은 아니므로 공동의 흔적은 관계를 깎는다.
            if (!caredIds.Contains(maid.MaidId))
                _maidFlow.ApplyNeglectedRelationPenalty(campaign, maid);
        }

        // 후유증 하루 경과. 오늘 안정을 받았다면 첫 항목은 이미 진행된 것으로 본다.
        for (int i = 0; i < present.Count; i++)
        {
            MaidStateV3 maid = present[i];
            _maidFlow.AdvanceAftereffects(campaign, maid, caredIds.Contains(maid.MaidId));
        }

        // 심야 대화 후, 오늘 손님 중 가장 모르는 쪽부터 분석한다.
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