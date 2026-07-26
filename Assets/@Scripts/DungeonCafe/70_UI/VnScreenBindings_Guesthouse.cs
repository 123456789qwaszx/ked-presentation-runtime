using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 게스트하우스 루프가 요구하는 화면 요청을 기존 패널 스택 위에 올린다.
///
/// 플로우는 await 로 결과를 기다리고, 패널은 이벤트로 결과를 밀어 넣는다.
/// 대기는 기존 프레젠테이션 레이어와 동일하게 프레임 폴링으로 처리한다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private int _pendingApprovalIndex = -1;
    private bool _hasApprovalResult;

    private string _pendingMaidId;
    private bool _hasAssignmentResult;

    private NightPlan _pendingNightPlan;
    private bool _hasNightPlanResult;

    private bool _hasConfirmResult;

    private bool _hasBoardResult;
    private bool _isBoardOpen;

    private bool _hasCodexResult;
    private bool _hasEndingResult;

    // 리포트/엔딩이 참조하는 누적값. HUD 갱신 때마다 함께 받아 둔다.
    private int _hudTotalEnergy;
    private int _hudEnergyQuota;
    private CampaignState _endingCampaign;

    // 승인 패널은 한 접객 동안 열린 채로 유지되므로, 스택 상태가 아니라 자체 플래그로 추적한다.
    private bool _isApprovalPanelOpen;

    #region ReservationBoard / DayReport

    public async YarnTask PresentReservationBoardAsync(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
    {
        _hasBoardResult = false;

        UI.PushPanel<ReservationBoardPanel>(panel =>
        {
            BindPanel(panel, board =>
            {
                AddBinding(board,
                    p => p.OnBookingSelected += HandleBookingSelected,
                    p => p.OnBookingSelected -= HandleBookingSelected);
            });

            panel.Present(dayNumber, bookings);
        });

        _isBoardOpen = true;

        await YarnWait.UntilAsync(() => _hasBoardResult);

        Debug.Log("ShowResult");
        
        // 여기서 닫지 않는다. 이어지는 통화 노드가 재생되는 동안 게시판이 떠 있어야 한다.
        // 실제 닫기는 배정 요청 시점에 한다.
    }

    private void HandleBookingSelected(int index)
    {
        _hasBoardResult = true;
        Debug.Log("Booking selected");
    }

    /// <summary>게시판이 열려 있으면 닫는다. 통화 노드가 끝난 뒤 호출된다.</summary>
    private void CloseBoardIfOpen()
    {
        if (!_isBoardOpen)
            return;

        _isBoardOpen = false;
        ClosePanel();
    }

    public async YarnTask PresentDayReportAsync(DayCycleState day)
    {
        _hasConfirmResult = false;

        UI.PushPanel<DayReportPanel>(panel =>
        {
            BindPanel(panel, report =>
            {
                AddBinding(report,
                    p => p.OnConfirmed += HandleSettlementConfirmed,
                    p => p.OnConfirmed -= HandleSettlementConfirmed);
            });

            panel.Present(day, _hudTotalEnergy, _hudEnergyQuota);
        });

        await YarnWait.UntilAsync(() => _hasConfirmResult);

        ClosePanel();
    }

    #endregion

    #region MaidAssignment

    public async YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
    {
        CloseBoardIfOpen();

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

    #region Hud

    /// <summary>
    /// 캠페인이 시작될 때 한 번 올린다.
    /// 오버레이 레이어는 blocksRaycasts=false 로 올라가므로 대사 진행 입력을 가로채지 않는다.
    /// </summary>
    public void ShowGuesthouseHud()
    {
        UI.ShowOverlay<GuesthouseStatusOverlay>();
    }

    public void HideGuesthouseHud()
    {
        UI.HideOverlay<GuesthouseStatusOverlay>();
    }

    /// <summary>
    /// 노드를 재생하기 직전에 호출된다. 동기 갱신이므로 대사 시작이 밀리지 않는다.
    /// 오버레이가 아직 올라오지 않았으면 조용히 넘어간다.
    /// </summary>
    public void UpdateGuesthouseHud(in GuesthouseHudSnapshot snapshot)
    {
        _hudTotalEnergy = snapshot.EnergyTotal;
        _hudEnergyQuota = snapshot.EnergyQuota;

        GuesthouseStatusOverlay overlay = UI.GetUI<GuesthouseStatusOverlay>();

        if (overlay == null)
            return;

        overlay.Apply(snapshot);
    }

    #endregion

    #region Codex

    public async YarnTask PresentCodexAsync(IReadOnlyList<ServiceBookingState> bookings)
    {
        _hasCodexResult = false;

        UI.PushPanel<MonsterCodexPanel>(panel =>
        {
            BindPanel(panel, codex =>
            {
                AddBinding(codex,
                    p => p.OnCloseRequested += HandleCodexClosed,
                    p => p.OnCloseRequested -= HandleCodexClosed);
            });

            panel.Present(bookings);
        });

        await YarnWait.UntilAsync(() => _hasCodexResult);

        ClosePanel();
    }

    private void HandleCodexClosed() => _hasCodexResult = true;

    #endregion

    #region Ending

    /// <summary>
    /// 엔딩 노드 재생과 '함께' 떠 있어야 하므로 표시와 대기를 분리한다.
    /// 여기서 await 하면 노드가 끝날 때까지 화면이 비어 있게 된다.
    /// </summary>
    public void PresentEnding(CampaignEndingResult ending, CampaignState campaignState)
    {
        _hasEndingResult = false;

        UI.PushPanel<CampaignEndingPanel>(panel =>
        {
            BindPanel(panel, view =>
            {
                AddBinding(view,
                    p => p.OnDismissed += HandleEndingDismissed,
                    p => p.OnDismissed -= HandleEndingDismissed);
            });

            panel.Present(ending, _endingCampaign);
        });
    }

    /// <summary>엔딩 노드가 끝난 뒤에 확인 버튼을 연다.</summary>
    public async YarnTask WaitEndingDismissAsync()
    {
        CampaignEndingPanel panel = UI.GetUI<CampaignEndingPanel>();

        if (panel != null)
            panel.AllowDismiss();

        await YarnWait.UntilAsync(() => _hasEndingResult);

        ClosePanel();
        HideGuesthouseHud();
    }

    private void HandleEndingDismissed() => _hasEndingResult = true;

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
