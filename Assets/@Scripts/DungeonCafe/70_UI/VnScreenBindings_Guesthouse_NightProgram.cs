using System;
using System.Collections.Generic;
using Yarn.Unity;

// (밤 처리 계획)
// manageCount 명까지 (메이드, 안정/관리 붕괴) 를 담아 확정. 빈 목록 = 전원 방치.
public sealed partial class VnScreenBindings
{
    private bool _isWaitingNightPlan;
    private bool _hasNightPlanResult;
    private IReadOnlyList<NightChoiceV3> _pendingNightPlan;

    public async YarnTask<IReadOnlyList<NightChoiceV3>> RequestNightPlanAsync(NightPlanRequestV3 request)
    {
        if (_isWaitingNightPlan)
            throw new InvalidOperationException("밤 처리 선택을 이미 기다리고 있습니다.");

        RefreshGuesthouseHud("밤 - 처리 선택");

        _isWaitingNightPlan = true;
        _hasNightPlanResult = false;
        _pendingNightPlan = null;

        UI.PushPanel<NightProgramPanel>(panel =>
        {
            BindPanel(panel, ApplyNightProgramBindings);
            panel.Present(request);
        });

        await AsyncWait.UntilAsync(() => _hasNightPlanResult);

        ClosePanel();
        _isWaitingNightPlan = false;

        return _pendingNightPlan;
    }

    private void ApplyNightProgramBindings(NightProgramPanel panel)
    {
        AddBinding(panel,
            p => p.OnPlanConfirmed += HandleNightPlanConfirmed,
            p => p.OnPlanConfirmed -= HandleNightPlanConfirmed);
    }

    private void HandleNightPlanConfirmed(IReadOnlyList<NightChoiceV3> plan)
    {
        _pendingNightPlan = plan;
        _hasNightPlanResult = true;
    }
}
