using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 게스트하우스 전용 화면 요청 접점.
/// VnScreenBindings 가 구현하며, 테스트/헤드리스 구동에서는 더미 구현으로 교체한다.
/// </summary>
public interface IGuesthouseScreenBindings
{
    // ---- 상시 표시 ----

    void ShowGuesthouseHud();

    void HideGuesthouseHud();

    void UpdateGuesthouseHud(in GuesthouseHudSnapshot snapshot);

    // ---- 패널 ----

    YarnTask PresentReservationBoardAsync(int dayNumber, IReadOnlyList<ServiceBookingState> bookings);

    YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request);

    YarnTask<int> RequestActionApprovalAsync(ServiceApprovalRequest request);

    void NotifyControlLost(ServiceSessionState session);

    YarnTask PresentSettlementAsync(ServiceSettlementResult result);

    YarnTask PresentDayReportAsync(DayCycleState day);

    YarnTask<NightPlan> RequestNightPlanAsync(NightPlanRequest request);

    /// <summary>업무수첩. 확정된 개체만 열람할 수 있다.</summary>
    YarnTask PresentCodexAsync(IReadOnlyList<ServiceBookingState> bookings);

    /// <summary>엔딩 표시. 엔딩 노드 재생과 함께 떠 있어야 한다.</summary>
    void PresentEnding(CampaignEndingResult ending, CampaignState campaign);

    /// <summary>엔딩 노드가 끝난 뒤 확인 입력을 기다린다.</summary>
    YarnTask WaitEndingDismissAsync();
}