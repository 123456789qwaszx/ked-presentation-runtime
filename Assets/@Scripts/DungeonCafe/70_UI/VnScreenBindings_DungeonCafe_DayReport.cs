using Yarn.Unity;

public sealed partial class VnScreenBindings
{
    private bool _hasDayReportResult;

    public async YarnTask PresentDayReportAsync(CampaignState campaign, DayState day, bool quotaMet)
    {
        RefreshDungeonCafeHud("하루 마감");

        _hasDayReportResult = false;

        UI.PushPanel<DayReportPanel>(panel =>
        {
            BindPanel(panel, ApplyDayReportBindings);
            panel.Present(campaign, day, quotaMet);
        });

        await AsyncWait.UntilAsync(() => _hasDayReportResult);

        ClosePanel();
    }

    private void ApplyDayReportBindings(DayReportPanel panel)
    {
        AddBinding(panel,
            p => p.OnConfirmed += HandleDayReportConfirmed,
            p => p.OnConfirmed -= HandleDayReportConfirmed);
    }

    private void HandleDayReportConfirmed()
    {
        _hasDayReportResult = true;
    }
}