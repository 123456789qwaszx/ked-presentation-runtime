using System;
using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 행동 승인. (v3 §2)
///
/// 승인 요청마다 새로 열리고, 플레이어가 옵션 하나를 (능력 예약과 함께) 고르면 닫힌다.
/// 승인 직후에는 연출 노드가 재생되므로 패널이 화면을 덮고 있으면 안 된다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _isWaitingApproval;
    private bool _hasApprovalResult;
    private int _pendingApprovalIndex;
    private IReadOnlyList<string> _pendingApprovalAbilities;

    private bool _isApprovalPanelOpen;

    public async YarnTask<ApprovalResponse> RequestApprovalAsync(ApprovalRequest request)
    {
        if (_isWaitingApproval)
            throw new InvalidOperationException("행동 승인을 이미 기다리고 있습니다.");

        RefreshDungeonCafeHud(request.Session, "접객 중");

        _isWaitingApproval = true;
        _hasApprovalResult = false;
        _pendingApprovalIndex = 0;
        _pendingApprovalAbilities = null;

        OpenActionApprovalPanel(request);

        try
        {
            await AsyncWait.UntilAsync(() => _hasApprovalResult);
        }
        finally
        {
            // 정상 선택이든 세션 취소든 패널이 남지 않게 한다.
            CloseApprovalPanelIfOpen();
            _isWaitingApproval = false;
        }

        return new ApprovalResponse(_pendingApprovalIndex, _pendingApprovalAbilities);
    }

    /// <summary>
    /// 통제 상실(심층 진입) 통보. 승인 패널이 열려 있으면 입력만 막는다.
    /// 자동 사건 동안의 상태 표시는 상시 HUD 가 담당한다.
    /// </summary>
    public void NotifyControlLost()
    {
        if (!_isApprovalPanelOpen)
            return;

        MaidActionApprovalPanel panel = UI.GetUI<MaidActionApprovalPanel>();

        if (panel != null)
            panel.LockForControlLoss();
    }

    private void CloseApprovalPanelIfOpen()
    {
        if (!_isApprovalPanelOpen)
            return;

        _isApprovalPanelOpen = false;
        ClosePanel();
    }

    private void OpenActionApprovalPanel(ApprovalRequest request)
    {
        // 앞선 요청이 어떤 이유로든 남아 있으면 먼저 정리한다.
        CloseApprovalPanelIfOpen();

        UI.PushPanel<MaidActionApprovalPanel>(panel =>
        {
            BindPanel(panel, ApplyActionApprovalBindings);
            panel.Present(request);
        });

        _isApprovalPanelOpen = true;
    }

    private void ApplyActionApprovalBindings(MaidActionApprovalPanel panel)
    {
        AddBinding(panel,
            p => p.OnOptionApproved += HandleOptionApproved,
            p => p.OnOptionApproved -= HandleOptionApproved);
    }

    private void HandleOptionApproved(int index, IReadOnlyList<string> abilityIds)
    {
        _pendingApprovalIndex = index;
        _pendingApprovalAbilities = abilityIds;
        _hasApprovalResult = true;
    }
}
