using Yarn.Unity;

/// <summary>
/// 행동 승인. 접객 세션 내내 열린 채로 유지되는 유일한 패널이다.
///
/// 비트마다 새로 여는 대신 이미 떠 있는 패널에 Present 만 다시 밀어 넣는다.
/// 패널 스택은 이 유지 상태를 표현하지 못하므로 _isApprovalPanelOpen 으로 따로 추적한다.
/// 닫기는 결산 패널이 열릴 때 CloseApprovalPanelIfOpen 으로 처리한다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private int _pendingApprovalIndex = -1;
    private bool _hasApprovalResult;

    private bool _isApprovalPanelOpen;

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
                BindPanel(panel, ApplyActionApprovalBindings);
                panel.Present(request);
            });

            _isApprovalPanelOpen = true;
        }

        await AsyncWait.UntilAsync(() => _hasApprovalResult);

        return _pendingApprovalIndex;
    }

    /// <summary>
    /// 통제 상실 통보. 패널을 닫지 않고 잠근다.
    /// 이후 자동 사건이 재생되는 동안에도 무엇이 진행 중인지는 계속 보여야 한다.
    /// </summary>
    public void NotifyControlLost(ServiceSessionState session)
    {
        if (!_isApprovalPanelOpen)
            return;

        MaidActionApprovalPanel panel = UI.GetUI<MaidActionApprovalPanel>();

        if (panel != null)
            panel.LockForControlLoss();
    }

    /// <summary>세션이 끝날 때 결산 패널이 호출한다.</summary>
    private void CloseApprovalPanelIfOpen()
    {
        if (!_isApprovalPanelOpen)
            return;

        _isApprovalPanelOpen = false;
        ClosePanel();
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
