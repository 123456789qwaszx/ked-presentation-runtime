using System;
using System.Threading.Tasks;

/// <summary>
/// 행동 승인.
///
/// 승인 요청마다 새로 열리고, 플레이어가 하나를 고르면 닫힌다.
/// 승인 직후에는 연출 노드가 재생되므로 패널이 화면을 덮고 있으면 안 된다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _isWaitingApproval;
    private bool _hasApprovalResult;
    private int _pendingApprovalIndex;

    private bool _isApprovalPanelOpen;

    public async Task<int> RequestActionApprovalAsync(ServiceApprovalRequest request)
    {
        if (_isWaitingApproval)
            throw new InvalidOperationException("행동 승인을 이미 기다리고 있습니다.");

        _isWaitingApproval = true;
        _hasApprovalResult = false;
        _pendingApprovalIndex = 0;

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

        return _pendingApprovalIndex;
    }

    /// <summary>
    /// 통제 상실 통보.
    ///
    /// 승인 패널은 이미 닫힌 뒤이므로 여기서는 할 일이 없다.
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

    private void OpenActionApprovalPanel(ServiceApprovalRequest request)
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

    private void HandleOptionApproved(int index)
    {
        _pendingApprovalIndex = index;
        _hasApprovalResult = true;
    }
}