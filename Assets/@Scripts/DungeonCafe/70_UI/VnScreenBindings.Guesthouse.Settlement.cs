using Yarn.Unity;

/// <summary>
/// 접객 결산. 슬롯 하나가 끝날 때마다 열린다.
///
/// 세션 내내 유지되던 승인 패널을 먼저 걷어낸 뒤 올린다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasSettlementResult;

    public async YarnTask PresentSettlementAsync(ServiceSettlementResult result)
    {
        CloseApprovalPanelIfOpen();

        _hasSettlementResult = false;

        UI.PushPanel<ServiceSettlementPanel>(panel =>
        {
            BindPanel(panel, ApplySettlementBindings);
            panel.Present(result);
        });

        await AsyncWait.UntilAsync(() => _hasSettlementResult);

        ClosePanel();
    }

    private void ApplySettlementBindings(ServiceSettlementPanel panel)
    {
        AddBinding(panel,
            p => p.OnConfirmed += HandleSettlementConfirmed,
            p => p.OnConfirmed -= HandleSettlementConfirmed);
    }

    private void HandleSettlementConfirmed()
    {
        _hasSettlementResult = true;
    }
}
