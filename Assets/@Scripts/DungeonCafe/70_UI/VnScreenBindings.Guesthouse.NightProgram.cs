using Yarn.Unity;

/// <summary>
/// 밤 처리 선택. 회복과 관리 붕괴 중 하나를 고른다.
///
/// 선택하지 않고 넘어갈 수 있으므로 기본값은 NightPlan.None 이다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private NightPlan _pendingNightPlan;
    private bool _hasNightPlanResult;

    public async YarnTask<NightPlan> RequestNightPlanAsync(NightPlanRequest request)
    {
        _hasNightPlanResult = false;
        _pendingNightPlan = NightPlan.None;

        UI.PushPanel<NightProgramPanel>(panel =>
        {
            BindPanel(panel, ApplyNightProgramBindings);
            panel.Present(request);
        });

        await AsyncWait.UntilAsync(() => _hasNightPlanResult);

        ClosePanel();

        return _pendingNightPlan;
    }

    private void ApplyNightProgramBindings(NightProgramPanel panel)
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
}
