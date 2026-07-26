using System.Collections.Generic;
using Yarn.Unity;

// (밤 진행)
// 상태 확인 → 한 명에게 회복 또는 붕괴 유도 → 준비된 행동 이벤트 출력 → 메이드 간 대화
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
        
        campaign.TryFindMaid(plan.MaidId, out MaidRuntimeState maid);
        NightProgramResult result = RunProgram(maid, plan);

        _screens.UpdateGuesthouseHud(
            GuesthouseHudSnapshot.ForNight(campaign, dayNumber, maid,
                plan.Kind == NightProgramKind.ManagedRelease ? "관리된 붕괴" : "회복 처리"));

        await _nodes.PlayNodeAsync(GuesthouseNodeNaming.NightProgram(plan.MaidId, plan.Kind, plan.Axis));
        
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
