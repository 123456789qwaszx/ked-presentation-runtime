using System.Collections.Generic;
using Yarn.Unity;

// 하루 진행.
// 게시판 목록 -> (손님 선택 -> 통화 확정 -> 배정 -> 접객 -> 결산) 3회 -> 하루 리포트 -> 밤
// 상태 전이는 전부 DayCycleState 가 소유. 이 클래스는 순서와 대기만 담당.
public sealed class DayCycleFlow
{
    private readonly BookingPlanner _bookingPlanner;
    private readonly ServiceSessionFlow _sessionFlow;
    private readonly NightPhaseFlow _nightFlow;

    private readonly VnScreenBindings _screens;
    private readonly ScenarioNodeRunner _nodes;

    private readonly List<MaidRuntimeState> _candidateBuffer = new();

    public DayCycleFlow(
        BookingPlanner bookingPlanner,
        ServiceSessionFlow sessionFlow,
        NightPhaseFlow nightFlow,
        VnScreenBindings screens,
        ScenarioNodeRunner nodes)
    {
        _bookingPlanner = bookingPlanner;
        _sessionFlow = sessionFlow;
        _nightFlow = nightFlow;
        _screens = screens;
        _nodes = nodes;
    }

    public async YarnTask RunDayAsync(CampaignState campaign)
    {
        // 인터넷 게시판에 오늘 방문 희망 몬스터 목록이 올라온다. 종족과 겉모습만 보인다.
        campaign.BeginDay(_bookingPlanner.CreateDailyBookings(campaign.NextDayNumber));
        DayCycleState dayCycle = campaign.CurrentDay;

        while (dayCycle.HasPendingBooking)
        {
            // 게시판에서 오늘 받을 손님을 고른다
            int bookingIndex = 
                await _screens.RequestReservationSelectionAsync(dayCycle.DayNumber, dayCycle.Bookings);

            ServiceBookingState booking = dayCycle.GetBooking(bookingIndex);

            // 전화로 예약 확정. 이 시점에 대응 타입이 수첩에 기재된다
            await _nodes.PlayNodeAsync(booking.Monster.PhoneCallNodeName);
            campaign.ConfirmBookingByPhone(booking);

            // 담당 메이드 배정
            IReadOnlyList<MaidRuntimeState> candidates =
                campaign.CollectAssignableMaids(_candidateBuffer);

            var request = new MaidAssignmentRequest(booking, candidates);
            string maidId = await _screens.RequestMaidAssignmentAsync(request);

            campaign.TryFindMaid(maidId, out MaidRuntimeState maid);

            // 격리실 접객 -> 반응 점수 x 붕괴 배율 결산
            ServiceSettlementResult result = await _sessionFlow.RunAsync(campaign, booking, maid);
            dayCycle.CompleteSlot(booking, result);
        }

        // 오늘 3회 접객 종료
        await _screens.PresentDayReportAsync(dayCycle);

        // 밤: 메이드 회복 또는 붕괴 유도
        await _nightFlow.RunNightAsync(campaign, dayCycle.DayNumber);

        campaign.CompleteDay();
    }
}