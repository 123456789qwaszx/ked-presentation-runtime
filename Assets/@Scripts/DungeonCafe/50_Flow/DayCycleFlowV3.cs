using System;
using System.Collections.Generic;
using Yarn.Unity;

// 하루 진행.
// 게시판 확인 -> 예약 통화 -> 메이드 배정 -> 접객과 결산 반복
// -> 하루 리포트 -> 즉시 엔딩이 아니면 밤 진행.
// 이 클래스는 하루의 순서와 대기만 담당한다.
public sealed class DayCycleFlowV3
{
    private readonly DailyMonsterSelectorV3 _monsterSelector;
    private readonly ServiceSessionFlowV3 _sessionFlow;
    private readonly NightPhaseFlowV3 _nightFlow;

    private readonly VnScreenBindings _screens;
    private readonly INodePlayerV3 _nodes;

    public DayCycleFlowV3(
        DailyMonsterSelectorV3 monsterSelector,
        ServiceSessionFlowV3 sessionFlow,
        NightPhaseFlowV3 nightFlow,
        VnScreenBindings screens,
        INodePlayerV3 nodes)
    {
        _monsterSelector = monsterSelector;
        _sessionFlow = sessionFlow;
        _nightFlow = nightFlow;
        _screens = screens;
        _nodes = nodes;
    }

    public async YarnTask RunDayAsync(CampaignStateV3 campaign)
    {
        int dayNumber = campaign.CurrentDayNumber;
        CampaignDayPlan plan = RequireDayPlan(campaign, dayNumber);

        List<MonsterProfileV3> bookings =
            _monsterSelector.SelectForDay(dayNumber, plan.ServiceSlots);

        DayStateV3 dayState = OpenDay(campaign, plan, bookings);

        // 오늘 방문할 손님 목록을 먼저 확인한다.
        await _screens.PresentBoardAsync(dayNumber, bookings, campaign);

        // 첫 통화인 개체만 통화 노드와 이해도 보상을 처리한다.
        await ConfirmBookingsByPhoneAsync(campaign, bookings);

        // 예약 순서대로 배정과 접객을 진행한다.
        await RunServiceSlotsAsync(campaign, dayState, bookings);

        // 오늘 수입으로 할당을 판정하고 리포트를 확인한다.
        await PresentDayReportAsync(campaign, dayState);

        // 낮에 즉시 엔딩이 확정됐다면 밤은 진행하지 않는다.
        if (EndingResolverV3.ResolveImmediate(campaign) != EndingKindV3.None)
            return;

        await _nightFlow.RunNightAsync(campaign, dayState);
    }

    private static CampaignDayPlan RequireDayPlan(
        CampaignStateV3 campaign,
        int dayNumber)
    {
        CampaignDayPlan plan = campaign.Content.GetDayPlan(dayNumber);

        return plan ?? throw new InvalidOperationException(
            $"캠페인 일차 계획을 찾지 못했습니다. day={dayNumber}");
    }

    private static DayStateV3 OpenDay(
        CampaignStateV3 campaign,
        CampaignDayPlan plan,
        IReadOnlyList<MonsterProfileV3> bookings)
    {
        campaign.Phase = CampaignPhaseV3.SlotBoundary;

        var dayState = new DayStateV3(plan.DayNumber, plan);

        for (int i = 0; i < bookings.Count; i++)
            dayState.BookedMonsterIds.Add(bookings[i].MonsterId);

        return dayState;
    }

    private async YarnTask ConfirmBookingsByPhoneAsync(
        CampaignStateV3 campaign,
        IReadOnlyList<MonsterProfileV3> bookings)
    {
        for (int i = 0; i < bookings.Count; i++)
        {
            MonsterProfileV3 monster = bookings[i];

            if (!campaign.Understanding.MarkPhoneCalled(monster.MonsterId))
                continue;

            await _nodes.PlayNodeAsync(monster.PhoneCallNodeName);

            campaign.Understanding.AddPoints(
                monster.MonsterId,
                campaign.Tuning.UnderstandingPerPhoneCall);
        }
    }

    private async YarnTask RunServiceSlotsAsync(
        CampaignStateV3 campaign,
        DayStateV3 dayState,
        IReadOnlyList<MonsterProfileV3> bookings)
    {
        for (int slot = 0; slot < bookings.Count; slot++)
        {
            campaign.Phase = CampaignPhaseV3.SlotBoundary;

            List<MaidStateV3> candidates =
                campaign.GetAssignable(dayState.DayNumber);

            if (candidates.Count == 0)
            {
                // 전원이 배정 불가라면 해당 슬롯은 수입 없이 소진된다.
                dayState.CompletedSlots++;
                continue;
            }

            MonsterProfileV3 monster = bookings[slot];

            string maidId = await _screens.RequestAssignmentAsync(
                monster,
                candidates,
                campaign);

            MaidStateV3 maid = ResolveAssignedMaid(
                campaign,
                candidates,
                maidId,
                dayState.DayNumber);

            await _sessionFlow.RunAsync(maid, monster);
            dayState.CompletedSlots++;

            if (EndingResolverV3.ResolveImmediate(campaign) != EndingKindV3.None)
                break;
        }
    }

    private static MaidStateV3 ResolveAssignedMaid(
        CampaignStateV3 campaign,
        IReadOnlyList<MaidStateV3> candidates,
        string maidId,
        int dayNumber)
    {
        MaidStateV3 maid = campaign.GetMaid(maidId);

        if (maid == null || !maid.CanBeAssigned(dayNumber))
            return candidates[0];

        return maid;
    }

    private async YarnTask PresentDayReportAsync(
        CampaignStateV3 campaign,
        DayStateV3 dayState)
    {
        bool quotaMet = campaign.Ledger.MeetsQuota(dayState.Plan.Quota);

        if (!quotaMet)
        {
            campaign.BankruptcyCount++;

            await _nodes.PlayNodeAsync(
                $"Quota_Warning_{campaign.BankruptcyCount}");
        }

        await _screens.PresentDayReportAsync(
            campaign,
            dayState,
            quotaMet);
    }
}
