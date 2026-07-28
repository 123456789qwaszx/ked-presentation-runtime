using System.Collections.Generic;
using Yarn.Unity;

// 하루 진행.
// 오늘 예약 생성 -> 게시판 확인 -> 예약 통화
// -> 배정과 접객 반복 -> 하루 보고 -> 밤.
public sealed class DayCycleFlow
{
    private readonly DailyMonsterSelector _monsterSelector;
    private readonly ServiceSessionFlow _sessionFlow;
    private readonly NightPhaseFlow _nightFlow;

    private readonly VnScreenBindings _screens;
    private readonly IDungeonCafeNodePlayer _dungeonCafeNodes;

    public DayCycleFlow(
        DailyMonsterSelector monsterSelector,
        ServiceSessionFlow sessionFlow,
        NightPhaseFlow nightFlow,
        VnScreenBindings screens,
        IDungeonCafeNodePlayer dungeonCafeNodes)
    {
        _monsterSelector = monsterSelector;
        _sessionFlow = sessionFlow;
        _nightFlow = nightFlow;
        _screens = screens;
        _dungeonCafeNodes = dungeonCafeNodes;
    }

    public async YarnTask RunDayAsync(CampaignState campaign)
    {
        CampaignDayPlan plan = campaign.Content.GetDayPlan(campaign.CurrentDayNumber);
        
        IReadOnlyList<MonsterProfile> bookings = 
            _monsterSelector.CreateDailyBookings(campaign.CurrentDayNumber);
        
        var dayState = new DayState(plan, bookings);

        // 오늘의 예약 게시판을 확인한다.
        int bookingIndex = 
            await _screens.PresentBoardAsync(dayState.DayNumber, dayState.Bookings, campaign);
        
        // 전화로 예약 확정. 이 시점에 대응 타입이 수첩에 기재된다
        MonsterProfile monster = dayState.Bookings[bookingIndex];

        if (campaign.RegisterPhoneCall(monster.MonsterId))
            await _dungeonCafeNodes.PlayNodeAsync(monster.PhoneCallNodeName);

        // 담당 메이드 배정
        List<MaidState> candidates = campaign.GetAssignable(dayState.DayNumber);
        
        string selectedMaidId =
            await _screens.RequestAssignmentAsync(monster, candidates, campaign);
        
        MaidState maid = campaign.GetMaid(selectedMaidId);
        
        // 격리실 접객
        await _sessionFlow.RunAsync(maid, monster);
        dayState.CompletedSlots++;
        
        if (EndingResolver.ResolveImmediate(campaign) == EndingKind.EmptyInn)
            return;

        // 수집 목표량 충족 체크
        bool quotaMet = campaign.Ledger.MeetsQuota(dayState.Plan.Quota);
        
        // if (!quotaMet)
        // {
        //     campaign.BankruptcyCount++;
        //     
        //     string warningNodeName = $"Quota_Warning_{campaign.BankruptcyCount}";
        //     await _dungeonCafeNodes.PlayNodeAsync(warningNodeName);
        // }
        
        // 오늘 접객 종료
        await _screens.PresentDayReportAsync(campaign, dayState, quotaMet);
            
        // if (EndingResolver.ResolveImmediate(campaign) == EndingKindV3.Bankruptcy)
        //     return;

        // 밤: 메이드 회복 또는 붕괴 유도
        await _nightFlow.RunNightAsync(campaign, dayState);
    }
}