using System;
using System.Collections.Generic;
using Yarn.Unity;

// 하루 진행.
// 게시판 확인 -> 예약 통화 -> 메이드 배정 -> 접객과 결산 반복
// -> 하루 리포트 -> 즉시 엔딩이 아니면 밤 진행.
//
// 이 클래스는 하루 동안의 진행 순서와 화면 대기만 담당한다.
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
        campaign.BeginDay(_monsterSelector.CreateDailyBookings(campaign.CurrentDayNumber));
        DayStateV3 dayState = campaign.CurrentDay;
        
        // 게시판에서 오늘 방문할 몬스터 목록을 확인.
        await _screens.PresentBoardAsync(dayState.DayNumber, dayState.Bookings, campaign);

        // 각 몬스터와 전화로 예약을 확정.
        for (int i = 0; i < dayState.Bookings.Count; i++)
        {
            MonsterProfileV3 monster = dayState.Bookings[i];

            bool isFirstPhoneCall =
                campaign.Understanding.MarkPhoneCalled(
                    monster.MonsterId);

            if (!isFirstPhoneCall)
                continue;

            await _nodes.PlayNodeAsync(
                monster.PhoneCallNodeName);

            campaign.Understanding.AddPoints(
                monster.MonsterId,
                campaign.Tuning.UnderstandingPerPhoneCall);
        }

        // 예약된 몬스터의 순서대로 배정과 접객을 진행한다.
        for (int slot = 0;
             slot < dayState.Bookings.Count;
             slot++)
        {
            campaign.Phase =
                CampaignPhaseV3.SlotBoundary;

            List<MaidStateV3> candidates =
                campaign.GetAssignable(
                    dayState.DayNumber);

            if (candidates.Count == 0)
            {
                dayState.CompletedSlots++;
                continue;
            }

            MonsterProfileV3 monster =
                dayState.Bookings[slot];

            string selectedMaidId =
                await _screens.RequestAssignmentAsync(
                    monster,
                    candidates,
                    campaign);

            MaidStateV3 maid =
                campaign.GetMaid(selectedMaidId);

            if (maid == null
                || !maid.CanBeAssigned(dayState.DayNumber))
            {
                maid = candidates[0];
            }

            await _sessionFlow.RunAsync(
                maid,
                monster);

            dayState.CompletedSlots++;

            if (EndingResolverV3.ResolveImmediate(campaign)
                != EndingKindV3.None)
            {
                break;
            }
        }

        // 오늘의 수입이 목표 할당량을 충족했는지 확인.
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

        // 낮에 즉시 엔딩이 결정되었다면
        // 밤 페이즈는 실행하지 않는다.
        if (EndingResolverV3.ResolveImmediate(campaign)
            != EndingKindV3.None)
        {
            return;
        }

        await _nightFlow.RunNightAsync(
            campaign,
            dayState);
    }
}