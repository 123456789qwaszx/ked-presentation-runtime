using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 하루 진행.
///   게시판 → 예약 확정 통화 → 담당 메이드 배정 → 접객 → 결산  (3회 반복)
///   → 하루 리포트 → 밤
/// </summary>
public sealed class DayCycleFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly IBookingPlanner _bookingPlanner;
    private readonly ServiceSessionFlow _sessionFlow;
    private readonly NightPhaseFlow _nightFlow;
    private readonly IDayPresentationPort _presentation;

    private readonly List<MaidRuntimeState> _candidateBuffer = new();

    public ProgressionTuning Tuning => _content.Tuning;

    public DayCycleFlow(
        GuesthouseContentDB content,
        IBookingPlanner bookingPlanner,
        ServiceSessionFlow sessionFlow,
        NightPhaseFlow nightFlow,
        IDayPresentationPort presentation)
    {
        _content = content;
        _bookingPlanner = bookingPlanner;
        _sessionFlow = sessionFlow;
        _nightFlow = nightFlow;
        _presentation = presentation;
    }

    public async YarnTask RunDayAsync(CampaignState campaign)
    {
        DayCycleState day = campaign.BeginDay();

        await RunReservationPhaseAsync(campaign, day);

        while (day.HasRemainingSlot)
        {
            await RunSlotAsync(campaign, day);
            day.AdvanceSlot();
        }

        day.SetPhase(DayPhaseKind.DayReport);
        await _presentation.PresentDayReportAsync(day);

        day.SetPhase(DayPhaseKind.Night);
        await _nightFlow.RunNightAsync(campaign, day.DayNumber);

        day.SetPhase(DayPhaseKind.DayClosed);
        campaign.CompleteDay();
    }

    private async YarnTask RunReservationPhaseAsync(CampaignState campaign, DayCycleState day)
    {
        day.SetPhase(DayPhaseKind.ReservationBoard);

        IReadOnlyList<MonsterProfile> monsters =
            _bookingPlanner.PlanBookings(campaign, day.DayNumber, Tuning.ServicesPerDay);

        day.PostBookings(monsters);

        await _presentation.PresentReservationBoardAsync(day.DayNumber, day.Bookings);

        // 전화로 확정하는 시점에 종족 외의 대응 타입 정보가 업무 수첩에 기재된다.
        day.SetPhase(DayPhaseKind.ReservationCall);

        for (int i = 0; i < day.Bookings.Count; i++)
        {
            ServiceBookingState booking = day.Bookings[i];

            await _presentation.PresentReservationCallAsync(booking);

            booking.ConfirmByPhone();
            campaign.MarkSpeciesEncountered(booking.Monster.Species);
        }
    }

    private async YarnTask RunSlotAsync(CampaignState campaign, DayCycleState day)
    {
        ServiceBookingState booking = day.CurrentBooking;

        if (booking == null)
            return;

        day.SetPhase(DayPhaseKind.MaidAssignment);

        MaidRuntimeState maid = await ResolveAssignedMaidAsync(campaign, booking);

        if (maid == null)
        {
            UnityEngine.Debug.LogWarning(
                $"[DayCycleFlow] No assignable maid. day={day.DayNumber}, slot={booking.SlotIndex}");
            return;
        }

        booking.AssignMaid(maid.MaidId);
        maid.MarkAssigned(day.DayNumber);

        day.SetPhase(DayPhaseKind.ServiceSession);

        ServiceSettlementResult result = await _sessionFlow.RunAsync(
            maid,
            booking.Monster,
            day.DayNumber,
            booking.SlotIndex);

        if (result == null)
            return;

        day.SetPhase(DayPhaseKind.ServiceSettlement);
        day.CommitSettlement(result);
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
        string maidId = await _presentation.RequestMaidAssignmentAsync(request);

        if (campaign.TryFindMaid(maidId, out MaidRuntimeState maid) && maid.CanBeAssigned)
            return maid;

        return candidates[0];
    }
}
