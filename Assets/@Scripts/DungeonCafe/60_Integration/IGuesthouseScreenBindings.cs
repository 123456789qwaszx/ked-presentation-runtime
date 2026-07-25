using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 게스트하우스 전용 화면 요청 접점.
/// VnScreenBindings 가 구현하며, 테스트/헤드리스 구동에서는 더미 구현으로 교체한다.
/// </summary>
public interface IGuesthouseScreenBindings
{
    YarnTask PresentReservationBoardAsync(int dayNumber, IReadOnlyList<ServiceBookingState> bookings);

    YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request);

    YarnTask<int> RequestActionApprovalAsync(ServiceApprovalRequest request);

    void NotifyControlLost(ServiceSessionState session);

    YarnTask PresentSettlementAsync(ServiceSettlementResult result);

    YarnTask PresentDayReportAsync(DayCycleState day);

    YarnTask<NightPlan> RequestNightPlanAsync(NightPlanRequest request);
}
