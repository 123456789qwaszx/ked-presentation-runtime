using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 하루 진행.
///   게시판 → 예약 확정 통화 3건 → (배정 → 접객 → 결산) 3회 → 하루 리포트 → 밤
///
/// 상태 전이는 전부 DayCycleState 가 소유한다. 이 클래스는 순서와 대기만 담당한다.
/// </summary>
public sealed class DayCycleFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly IBookingPlanner _bookingPlanner;
    private readonly ServiceSessionFlow _sessionFlow;
    private readonly NightPhaseFlow _nightFlow;

    private readonly VnScreenBindings _screens;
    private readonly ScenarioNodeRunner _nodes;

    private readonly List<MaidRuntimeState> _candidateBuffer = new();

    public ProgressionTuning Tuning => _content.Tuning;

    public DayCycleFlow(
        GuesthouseContentDB content,
        IBookingPlanner bookingPlanner,
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
        campaign.BeginDay();
        DayCycleState day = campaign.CurrentDay;

        await RunReservationsAsync(campaign, day);

        while (day.TryGetPendingSlot(out ServiceBookingState booking))
            await RunSlotAsync(campaign, day, booking);

        Enter(campaign, day, DayPhaseKind.DayReport);
        await _screens.PresentDayReportAsync(day);

        Enter(campaign, day, DayPhaseKind.Night);
        await _nightFlow.RunNightAsync(campaign, day.DayNumber);

        campaign.CompleteDay();
    }

    private async YarnTask RunReservationsAsync(CampaignState campaign, DayCycleState day)
    {
        Enter(campaign, day, DayPhaseKind.ReservationBoard);

        day.PostBookings(_bookingPlanner.PlanBookings(campaign, day.DayNumber, Tuning.ServicesPerDay));
        await _screens.RequestReservationSelectionAsync(day.DayNumber, day.Bookings);

        Enter(campaign, day, DayPhaseKind.ReservationCall);

        for (int i = 0; i < day.Bookings.Count; i++)
        {
            ServiceBookingState booking = day.Bookings[i];

            await _nodes.PlayNodeAsync(booking.Monster.PhoneCallNodeName);
            campaign.ConfirmBookingByPhone(booking);
        }
    }

    private async YarnTask RunSlotAsync(
        CampaignState campaign,
        DayCycleState day,
        ServiceBookingState booking)
    {
        Enter(campaign, day, DayPhaseKind.MaidAssignment);

        MaidRuntimeState maid = await ResolveAssignedMaidAsync(campaign, booking);

        day.AssignMaid(booking, maid);

        Enter(campaign, day, DayPhaseKind.ServiceSession);

        ServiceSettlementResult result = await _sessionFlow.RunAsync(campaign, booking, maid);


        Enter(campaign, day, DayPhaseKind.ServiceSettlement);
        day.CompleteSlot(result);
    }

    private async YarnTask<MaidRuntimeState> ResolveAssignedMaidAsync(
        CampaignState campaign,
        ServiceBookingState booking)
    {
        IReadOnlyList<MaidRuntimeState> candidates =
            campaign.CollectAssignableMaids(_candidateBuffer);

        if (candidates.Count == 0)
            return null;

        MaidAssignmentRequest request = new(booking, candidates, Tuning);
        string maidId = await _screens.RequestMaidAssignmentAsync(request);

        if (campaign.TryFindMaid(maidId, out MaidRuntimeState maid) && maid.CanBeAssigned)
            return maid;

        return candidates[0];
    }

    /// <summary>단계 진입. 페이즈 기록과 표시 갱신이 항상 함께 일어난다.</summary>
    private void Enter(CampaignState campaign, DayCycleState day, DayPhaseKind phase)
    {
        day.SetPhase(phase);

        _screens.UpdateGuesthouseHud(
            GuesthouseHudSnapshot.ForDay(campaign, day, DayPhaseLabels.Of(phase)));
    }
}