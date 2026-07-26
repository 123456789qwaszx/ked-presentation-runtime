using Yarn.Unity;

/// <summary>
/// 하루 리포트. 3슬롯이 모두 끝난 뒤 한 번 열린다.
///
/// 누적 에너지와 목표치는 HUD 갱신 때 받아 둔 값을 그대로 쓴다(GuesthouseHudSnapshot 참조).
/// 리포트 시점에는 DayCycleState 만 넘어오므로 캠페인 누적값을 알 수 없기 때문이다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasDayReportResult;

    public async YarnTask PresentDayReportAsync(DayCycleState day)
    {
        _hasDayReportResult = false;

        UI.PushPanel<DayReportPanel>(panel =>
        {
            BindPanel(panel, ApplyDayReportBindings);
            panel.Present(day, _hudTotalEnergy, _hudEnergyQuota);
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
