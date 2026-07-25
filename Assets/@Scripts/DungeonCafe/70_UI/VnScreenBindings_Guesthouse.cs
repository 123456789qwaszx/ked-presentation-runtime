using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 게스트하우스 루프가 요구하는 화면 요청을 기존 패널 스택 위에 올린다.
///
/// 플로우는 await 로 결과를 기다리고, 패널은 이벤트로 결과를 밀어 넣는다.
/// 대기는 기존 프레젠테이션 레이어와 동일하게 프레임 폴링으로 처리한다.
/// </summary>
public sealed partial class VnScreenBindings : IGuesthouseScreenBindings
{
    private int _pendingApprovalIndex = -1;
    private bool _hasApprovalResult;

    private string _pendingMaidId;
    private bool _hasAssignmentResult;

    private NightPlan _pendingNightPlan;
    private bool _hasNightPlanResult;

    private bool _hasConfirmResult;

    // 승인 패널은 한 접객 동안 열린 채로 유지되므로, 스택 상태가 아니라 자체 플래그로 추적한다.
    private bool _isApprovalPanelOpen;

    #region ReservationBoard / DayReport

    public async YarnTask PresentReservationBoardAsync(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
    {
        string body = BuildReservationBoardBody(bookings);

        await PresentConfirmAsync(
            title: $"{dayNumber}일차 예약 문의",
            body: body,
            confirmLabel: "전화 걸기");
    }

    public async YarnTask PresentDayReportAsync(DayCycleState day)
    {
        string body =
            $"확보 에너지: {day.EnergyEarned}\n" +
            $"성사 실패: {day.CountFailedBookings()}건\n" +
            $"통제 상실: {day.CountIncidents()}건";

        await PresentConfirmAsync(
            title: $"{day.DayNumber}일차 업무 종료",
            body: body,
            confirmLabel: "밤으로");
    }

    private static string BuildReservationBoardBody(IReadOnlyList<ServiceBookingState> bookings)
    {
        System.Text.StringBuilder builder = new();

        for (int i = 0; i < bookings.Count; i++)
        {
            MonsterProfile monster = bookings[i].Monster;

            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append($"[{monster.Species}] {monster.DisplayName}\n{monster.ReservationPostText}\n");
        }

        return builder.ToString();
    }

    #endregion

    #region MaidAssignment

    public async YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
    {
        _hasAssignmentResult = false;
        _pendingMaidId = null;

        UI.PushPanel<MaidAssignmentPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            panel.Present(request);
        });

        await YarnWait.UntilAsync(() => _hasAssignmentResult);

        ClosePanel();

        return _pendingMaidId;
    }

    private void ApplyBindings(MaidAssignmentPanel panel)
    {
        AddBinding(panel,
            p => p.OnMaidSelected += HandleMaidSelected,
            p => p.OnMaidSelected -= HandleMaidSelected);
    }

    private void HandleMaidSelected(string maidId)
    {
        _pendingMaidId = maidId;
        _hasAssignmentResult = true;
    }

    #endregion

    #region ActionApproval

    public async YarnTask<int> RequestActionApprovalAsync(ServiceApprovalRequest request)
    {
        _hasApprovalResult = false;
        _pendingApprovalIndex = -1;

        if (_isApprovalPanelOpen)
        {
            MaidActionApprovalPanel existing = UI.GetUI<MaidActionApprovalPanel>();

            if (existing != null)
                existing.Present(request);
        }
        else
        {
            UI.PushPanel<MaidActionApprovalPanel>(panel =>
            {
                BindPanel(panel, ApplyBindings);
                panel.Present(request);
            });

            _isApprovalPanelOpen = true;
        }

        await YarnWait.UntilAsync(() => _hasApprovalResult);

        return _pendingApprovalIndex;
    }

    public void NotifyControlLost(ServiceSessionState session)
    {
        if (!_isApprovalPanelOpen)
            return;

        MaidActionApprovalPanel panel = UI.GetUI<MaidActionApprovalPanel>();

        if (panel != null)
            panel.LockForControlLoss();
    }

    private void ApplyBindings(MaidActionApprovalPanel panel)
    {
        AddBinding(panel,
            p => p.OnOptionApproved += HandleOptionApproved,
            p => p.OnOptionApproved -= HandleOptionApproved);
    }

    private void HandleOptionApproved(int index)
    {
        _pendingApprovalIndex = index;
        _hasApprovalResult = true;
    }

    #endregion

    #region Settlement

    public async YarnTask PresentSettlementAsync(ServiceSettlementResult result)
    {
        // 접객 중 유지하던 승인 패널을 먼저 걷어낸다.
        CloseApprovalPanelIfOpen();

        _hasConfirmResult = false;

        UI.PushPanel<ServiceSettlementPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            panel.Present(result);
        });

        await YarnWait.UntilAsync(() => _hasConfirmResult);

        ClosePanel();
    }

    private void ApplyBindings(ServiceSettlementPanel panel)
    {
        AddBinding(panel,
            p => p.OnConfirmed += HandleSettlementConfirmed,
            p => p.OnConfirmed -= HandleSettlementConfirmed);
    }

    private void HandleSettlementConfirmed()
    {
        _hasConfirmResult = true;
    }

    private void CloseApprovalPanelIfOpen()
    {
        if (!_isApprovalPanelOpen)
            return;

        ClosePanel();
        _isApprovalPanelOpen = false;
    }

    #endregion

    #region Night

    public async YarnTask<NightPlan> RequestNightPlanAsync(NightPlanRequest request)
    {
        _hasNightPlanResult = false;
        _pendingNightPlan = NightPlan.None;

        UI.PushPanel<NightProgramPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            panel.Present(request);
        });

        await YarnWait.UntilAsync(() => _hasNightPlanResult);

        ClosePanel();

        return _pendingNightPlan;
    }

    private void ApplyBindings(NightProgramPanel panel)
    {
        AddBinding(panel,
            p => p.OnPlanSelected += HandleNightPlanSelected,
            p => p.OnPlanSelected -= HandleNightPlanSelected);
    }

    private void HandleNightPlanSelected(NightPlan plan)
    {
        _pendingNightPlan = plan;
        _hasNightPlanResult = true;
    }

    #endregion

    #region Shared

    private async YarnTask PresentConfirmAsync(string title, string body, string confirmLabel)
    {
        _hasConfirmResult = false;

        UI.PushPanel<ConfirmPanel>(panel =>
        {
            BindPanel(panel, confirm =>
            {
                AddBinding(confirm,
                    p => p.ConfirmClicked += HandleSettlementConfirmed,
                    p => p.ConfirmClicked -= HandleSettlementConfirmed);
            });

            panel.Present(
                title: title,
                body: body,
                confirmLabel: confirmLabel,
                cancelLabel: string.Empty);
        });

        await YarnWait.UntilAsync(() => _hasConfirmResult);

        ClosePanel();
    }

    #endregion
}
