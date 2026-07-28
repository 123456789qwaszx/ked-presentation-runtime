using Yarn.Unity;

// 접객 결산. 슬롯 하나가 끝날 때마다 열림.
public sealed partial class VnScreenBindings
{
    private bool _hasSettlementResult;

    public async YarnTask PresentSettlementAsync(ServiceSessionState session, SettlementResult result)
    {
        CloseApprovalPanelIfOpen();
        RefreshDungeonCafeHud(session, "결산");

        _hasSettlementResult = false;

        UI.PushPanel<ServiceSettlementPanel>(panel =>
        {
            BindPanel(panel, ApplySettlementBindings);
            panel.Present(session, result);
        });

        await AsyncWait.UntilAsync(() => _hasSettlementResult);

        ClosePanel();
        RefreshDungeonCafeHud("접객 사이");
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