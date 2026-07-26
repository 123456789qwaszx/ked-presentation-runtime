using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 밤 진행.
///   상태 확인 → 한 명에게 회복 또는 관리 붕괴 적용 → 준비된 숙련 이벤트 소화 → 메이드 간 대화
///
/// 회복은 안전하지만 낮에 쌓인 긴장을 그대로 버린다.
/// 관리 붕괴는 통제된 상태에서 끝까지 완료시켜, 붕괴도를 회수하면서 숙련으로 전환한다.
/// 어느 쪽을 고르는지가 다음 날의 배율과 성장 속도를 동시에 결정한다.
/// </summary>
public sealed class NightPhaseFlow
{
    private readonly GuesthouseContentDB _content;

    private readonly VnScreenBindings _screens;
    private readonly ScenarioNodeRunner _nodes;

    public ProgressionTuning Tuning => _content.Tuning;

    public NightPhaseFlow(
        GuesthouseContentDB content,
        VnScreenBindings screens,
        ScenarioNodeRunner nodes)
    {
        _content = content;
        _screens = screens;
        _nodes = nodes;
    }

    public async YarnTask RunNightAsync(CampaignState campaign, int dayNumber)
    {
        NightPlanRequest request = new(dayNumber, campaign.Maids, Tuning);
        _screens.UpdateGuesthouseHud(
            GuesthouseHudSnapshot.ForNight(campaign, dayNumber, null, "밤 처리"));

        NightPlan plan = await _screens.RequestNightPlanAsync(request);

        if (plan.IsValid && campaign.TryFindMaid(plan.MaidId, out MaidRuntimeState maid))
        {
            NightProgramResult result = RunProgram(maid, plan);
            campaign.TryFindMaid(plan.MaidId, out MaidRuntimeState programMaid);

            _screens.UpdateGuesthouseHud(
                GuesthouseHudSnapshot.ForNight(
                    campaign,
                    dayNumber,
                    programMaid,
                    plan.Kind == NightProgramKind.ManagedRelease ? "관리된 붕괴" : "회복 처리"));

            // 처리가 실패하면 재생할 본편이 없다. 노드 이름을 만들지 않고 그대로 넘어간다.
            if (result.IsSuccess)
            {
                await _nodes.PlayNodeAsync(
                    GuesthouseNodeNaming.NightProgram(plan.MaidId, plan.Kind, plan.Axis));
            }
        }

        await RunMasteryEventsAsync(campaign, dayNumber);

        await _nodes.PlayNodeAsync(GuesthouseNodeNaming.MaidConversation(dayNumber));
    }

    private NightProgramResult RunProgram(MaidRuntimeState maid, NightPlan plan)
    {
        return plan.Kind switch
        {
            NightProgramKind.ManagedRelease =>
                NightConversionRule.RunManagedRelease(maid, plan.Axis, Tuning),

            NightProgramKind.Care =>
                NightConversionRule.RunCare(maid, plan.Axis, Tuning),

            _ => NightProgramResult.Failed(plan.Kind, maid.MaidId, plan.Axis, "지정되지 않은 처리"),
        };
    }

    /// <summary>
    /// 경험치가 기준을 넘긴 트랙은 자동으로 레벨이 오르지 않는다.
    /// 여기서 이벤트를 실제로 소화해야 레벨업이 확정된다.
    /// </summary>
    private async YarnTask RunMasteryEventsAsync(CampaignState campaign, int dayNumber)
    {
        IReadOnlyList<MaidRuntimeState> maids = campaign.Maids;

        for (int i = 0; i < maids.Count; i++)
        {
            MaidRuntimeState maid = maids[i];

            if (maid.IsLost)
                continue;

            while (maid.TryFindReadyMasteryAxis(Tuning, out BurdenAxis axis))
            {
                int levelAfter = maid.GetMastery(axis).Level + 1;

                MasteryEventResult result = NightConversionRule.RunMasteryEvent(
                    maid,
                    axis,
                    Tuning,
                    GuesthouseNodeNaming.MasteryEvent(maid.MaidId, axis, levelAfter));

                if (!result.IsLevelUpCommitted)
                    break;

                _screens.UpdateGuesthouseHud(
                    GuesthouseHudSnapshot.ForNight(campaign, dayNumber, maid, "업무 숙련"));

                await _nodes.PlayNodeAsync(result.EventNodeName);
            }
        }
    }
}
