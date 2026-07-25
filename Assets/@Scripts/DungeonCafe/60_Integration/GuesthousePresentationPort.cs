using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 세 개의 플로우 포트를 Yarn 러너 + 화면 바인딩으로 연결하는 어댑터.
/// 플로우 레이어는 이 타입을 알지 못하고, 이 타입만 Unity/Yarn/UI 를 동시에 안다.
/// </summary>
public sealed class GuesthousePresentationPort :
    IServicePresentationPort,
    IDayPresentationPort,
    INightPresentationPort
{
    private readonly ScenarioNodeRunner _nodeRunner;
    private readonly IGuesthouseScreenBindings _screens;
    private readonly GuesthouseYarnContext _yarnContext;

    public GuesthousePresentationPort(
        ScenarioNodeRunner nodeRunner,
        IGuesthouseScreenBindings screens,
        GuesthouseYarnContext yarnContext = null)
    {
        _nodeRunner = nodeRunner;
        _screens = screens;
        _yarnContext = yarnContext;
    }

    // ---- IServicePresentationPort ----

    public void NotifySessionContext(ServiceSessionState session)
    {
        // 변수 저장소가 연결되지 않은 구성에서도 흐름은 그대로 진행되어야 한다.
        if (_yarnContext == null)
            return;

        _yarnContext.PushSession(session);
    }

    public YarnTask PlayNodeAsync(string nodeName)
        => _nodeRunner.PlayNodeAsync(nodeName);

    public YarnTask<int> RequestActionApprovalAsync(ServiceApprovalRequest request)
        => _screens.RequestActionApprovalAsync(request);

    public void NotifyControlLost(ServiceSessionState session)
        => _screens.NotifyControlLost(session);

    public YarnTask PresentSettlementAsync(ServiceSettlementResult result)
        => _screens.PresentSettlementAsync(result);

    // ---- IDayPresentationPort ----

    public YarnTask PresentReservationBoardAsync(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
        => _screens.PresentReservationBoardAsync(dayNumber, bookings);

    public YarnTask PresentReservationCallAsync(ServiceBookingState booking)
        => _nodeRunner.PlayNodeAsync(booking.Monster.PhoneCallNodeName);

    public YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
        => _screens.RequestMaidAssignmentAsync(request);

    public YarnTask PresentDayReportAsync(DayCycleState day)
        => _screens.PresentDayReportAsync(day);

    // ---- INightPresentationPort ----

    public YarnTask<NightPlan> RequestNightPlanAsync(NightPlanRequest request)
        => _screens.RequestNightPlanAsync(request);

    public YarnTask PlayNightProgramAsync(NightPlan plan, NightProgramResult result)
    {
        if (!result.IsSuccess)
            return PlayNothingAsync();

        return _nodeRunner.PlayNodeAsync(
            GuesthouseNodeNaming.NightProgram(plan.MaidId, plan.Kind, plan.Axis));
    }

    public YarnTask PlayMasteryEventAsync(MasteryEventResult result)
        => _nodeRunner.PlayNodeAsync(result.EventNodeName);

    public YarnTask PlayMaidConversationAsync(int dayNumber)
        => _nodeRunner.PlayNodeAsync(GuesthouseNodeNaming.MaidConversation(dayNumber));

    private static async YarnTask PlayNothingAsync()
    {
        await YarnTask.Yield();
    }
}
