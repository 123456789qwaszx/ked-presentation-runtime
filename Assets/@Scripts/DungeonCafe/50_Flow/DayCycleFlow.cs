using System.Collections.Generic;
using Yarn.Unity;

// 하루 진행.
// 게시판 → 예약 확정 통화 3건 → (배정 → 접객 → 결산) 3회 → 하루 리포트 → 밤
// 상태 전이는 전부 DayCycleState 가 소유. 이 클래스는 순서와 대기만 담당.
public sealed class DayCycleFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly BookingPlanner _bookingPlanner;
    private readonly ServiceSessionFlow _sessionFlow;
    private readonly NightPhaseFlow _nightFlow;

    private readonly VnScreenBindings _screens;
    private readonly ScenarioNodeRunner _nodes;

    private readonly List<MaidRuntimeState> _candidateBuffer = new();

    public ProgressionTuning Tuning => _content.Tuning;

    public DayCycleFlow(
        GuesthouseContentDB content,
        BookingPlanner bookingPlanner,
        ServiceSessionFlow sessionFlow,
        NightPhaseFlow nightFlow,
        VnScreenBindings screens,
        ScenarioNodeRunner nodes)
    {
        _content = content;
        _bookingPlanner = bookingPlanner;
        _sessionFlow = sessionFlow;
        _nightFlow = nightFlow;
        _screens = screens;
        _nodes = nodes;
    }

    public async YarnTask RunDayAsync(CampaignState campaign)
    {
        // 인터넷 게시판에 오늘 방문 희망 몬스터 예약 목록 출력
        campaign.BeginDay(_bookingPlanner.CreateDailyBookings(campaign.NextDayNumber));
        DayCycleState dayCycle = campaign.CurrentDay;

        await _screens.RequestReservationSelectionAsync(dayCycle.DayNumber, dayCycle.Bookings);

        // 예약 확정. 수첩 정보 갱신
        for (int i = 0; i < dayCycle.Bookings.Count; i++)
        {
            ServiceBookingState booking = dayCycle.Bookings[i];

            await _nodes.PlayNodeAsync(booking.Monster.PhoneCallNodeName);
            campaign.ConfirmBookingByPhone(booking);
        }

        // 접객 3회: 담당 메이드를 배정 -> 격리실 접객 진행 -> 반응 점수 × 붕괴 배율 결산
        while (dayCycle.TryGetPendingSlot(out ServiceBookingState pending))
        {
            IReadOnlyList<MaidRuntimeState> candidates =
                campaign.CollectAssignableMaids(_candidateBuffer);

            MaidAssignmentRequest request = new(pending, candidates, Tuning);
            string maidId = await _screens.RequestMaidAssignmentAsync(request);

            campaign.TryFindMaid(maidId, out MaidRuntimeState maid);

            dayCycle.AssignMaid(pending, maid);

            ServiceSettlementResult result = await _sessionFlow.RunAsync(campaign, pending, maid);
            dayCycle.CompleteSlot(result);
        }

        // 오늘 3회 접객 종료
        await _screens.PresentDayReportAsync(dayCycle);

        // 밤: 메이드 회복 또는 붕괴 유도
        await _nightFlow.RunNightAsync(campaign, dayCycle.DayNumber);

        campaign.CompleteDay();
    }
}