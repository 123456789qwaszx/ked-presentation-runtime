using System;
using System.Threading.Tasks;

/// <summary>
/// 밤 처리 선택. 회복과 관리 붕괴 중 하나를 고른다.
///
/// 선택하지 않고 넘어갈 수 있으므로 기본값은 NightPlan.None 이다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _isWaitingNightPlan;
    private bool _hasNightPlanResult;
    private NightPlan _pendingNightPlan;

    public async Task<NightPlan> RequestNightPlanAsync(NightPlanRequest request)
    {
        // 값 반환 패널만 검사를 둔다
        if (_isWaitingNightPlan)
            throw new InvalidOperationException("밤 처리 선택을 이미 기다리고 있습니다.");

        _isWaitingNightPlan = true;
        _hasNightPlanResult = false;
        _pendingNightPlan = default;

        OpenNightProgramPanel(request);

        await AsyncWait.UntilAsync(() => _hasNightPlanResult);

        ClosePanel();
        _isWaitingNightPlan = false;

        return _pendingNightPlan;
    }

    private void OpenNightProgramPanel(NightPlanRequest request)
    {
        UI.PushPanel<NightProgramPanel>(panel =>
        {
            BindPanel(panel, ApplyNightProgramBindings);
            panel.Present(request);
        });
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