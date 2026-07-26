using System.Threading.Tasks;

// 누적 에너지와 목표치는 HUD 갱신 때 받아 둔 값을 그대로 쓴다(GuesthouseHudSnapshot 참조).
// 리포트 시점에는 DayCycleState 만 넘어오므로 캠페인 누적값을 알 수 없기 때문이다.
public sealed partial class VnScreenBindings
{
    private bool _hasDayReportResult;

    public async Task PresentDayReportAsync(DayCycleState day)
    {
        _hasDayReportResult = false;

        OpenDayReportPanel(day);

        await AsyncWait.UntilAsync(() => _hasDayReportResult);

        ClosePanel();
    }

    private void OpenDayReportPanel(DayCycleState day)
    {
        UI.PushPanel<DayReportPanel>(panel =>
        {
            BindPanel(panel, ApplyDayReportBindings);
            panel.Present(day, _hudTotalEnergy, _hudEnergyQuota);
        });
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