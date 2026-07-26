using System;
using System.Threading.Tasks;

/// <summary>
/// 행동 승인. 접객 세션 내내 열린 채로 유지되는 유일한 패널이다.
///
/// 다른 패널과 달리 열기와 닫기가 한 메서드에 있지 않다.
/// 비트마다 닫았다 열면 화면이 깜빡이므로, 이미 떠 있으면 Present 만 다시 밀어 넣는다.
/// 실제 닫기는 세션이 끝나 결산 패널이 열릴 때 CloseApprovalPanelIfOpen 으로 처리한다.
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

        PresentActionApprovalPanel(request);

        await AsyncWait.UntilAsync(() => _hasApprovalResult);

        _isWaitingApproval = false;

        return _pendingApprovalIndex;
    }

    /// <summary>
    /// 통제 상실 통보. 패널을 닫지 않고 잠근다.
    /// 이후 자동 사건이 재생되는 동안에도 무엇이 진행 중인지는 계속 보여야 한다.
    /// </summary>
    public void NotifyControlLost()
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

    /// <summary>이미 떠 있으면 내용만 갈아 끼우고, 아니면 새로 올린다.</summary>
    private void PresentActionApprovalPanel(ServiceApprovalRequest request)
    {
        if (_isApprovalPanelOpen)
        {
            MaidActionApprovalPanel opened = UI.GetUI<MaidActionApprovalPanel>();

            if (opened != null)
                opened.Present(request);

            return;
        }

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