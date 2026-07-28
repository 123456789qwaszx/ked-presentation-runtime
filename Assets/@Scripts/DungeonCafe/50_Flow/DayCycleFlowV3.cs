using System.Collections.Generic;
using Yarn.Unity;

// 하루 진행.
// 오늘 예약 생성 -> 게시판 확인 -> 예약 통화
// -> 배정과 접객 반복 -> 하루 보고 -> 밤.
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
        CampaignDayPlan plan = campaign.Content.GetDayPlan(campaign.CurrentDayNumber);
        
        IReadOnlyList<MonsterProfileV3> bookings = 
            _monsterSelector.CreateDailyBookings(campaign.CurrentDayNumber);
        
        var dayState = new DayStateV3(plan, bookings);

        // 오늘의 예약 게시판을 확인한다.
        int bookingIndex = 
            await _screens.PresentBoardAsync(dayState.DayNumber, dayState.Bookings, campaign);
        
        // 전화로 예약 확정. 이 시점에 대응 타입이 수첩에 기재된다
        MonsterProfileV3 monster = dayState.Bookings[bookingIndex];

        if (campaign.RegisterPhoneCall(monster.MonsterId))
            await _nodes.PlayNodeAsync(monster.PhoneCallNodeName);

        // 담당 메이드 배정
        List<MaidStateV3> candidates = campaign.GetAssignable(dayState.DayNumber);
        
        string selectedMaidId =
            await _screens.RequestAssignmentAsync(monster, candidates, campaign);
        
        MaidStateV3 maid = campaign.GetMaid(selectedMaidId);
        
        // 격리실 접객
        await _sessionFlow.RunAsync(maid, monster);
        dayState.CompletedSlots++;
        
        if (EndingResolverV3.ResolveImmediate(campaign) == EndingKindV3.EmptyInn)
            return;

        // 수집 목표량 충족 체크
        bool quotaMet = campaign.Ledger.MeetsQuota(dayState.Plan.Quota);
        
        // if (!quotaMet)
        // {
        //     campaign.BankruptcyCount++;
        //     
        //     string warningNodeName = $"Quota_Warning_{campaign.BankruptcyCount}";
        //     await _nodes.PlayNodeAsync(warningNodeName);
        // }
        
        // 오늘 접객 종료
        await _screens.PresentDayReportAsync(campaign, dayState, quotaMet);
            
        // if (EndingResolverV3.ResolveImmediate(campaign) == EndingKindV3.Bankruptcy)
        //     return;

        // 밤: 메이드 회복 또는 붕괴 유도
        await _nightFlow.RunNightAsync(campaign, dayState);
    }
}