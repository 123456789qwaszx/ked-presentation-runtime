using System.Collections.Generic;
using Yarn.Unity;

// (밤 진행) 상태 확인 → 한 명에게 회복 또는 관리 붕괴 적용 → 준비된 숙련 이벤트 소화 → 메이드 대화
public sealed class NightPhaseFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly INightPresentationPort _presentation;

    public NightPhaseFlow(GuesthouseContentDB content, INightPresentationPort presentation)
    {
        _content = content;
        _presentation = presentation;
    }

    public async YarnTask RunNightAsync(CampaignState campaign, int dayNumber)
    {
        NightPlanRequest request = new(dayNumber, campaign.Maids, _content.Tuning);
        NightPlan plan = await _presentation.RequestNightPlanAsync(request);

        if (plan.IsValid && campaign.TryFindMaid(plan.MaidId, out MaidRuntimeState maid))
        {
            NightProgramResult result = RunProgram(maid, plan);
            await _presentation.PlayNightProgramAsync(plan, result);
        }

        await RunMasteryEventsAsync(campaign);

        await _presentation.PlayMaidConversationAsync(dayNumber);
    }

    private NightProgramResult RunProgram(MaidRuntimeState maid, NightPlan plan)
    {
        return plan.Kind switch
        {
            NightProgramKind.ManagedRelease =>
                NightConversionRule.RunManagedRelease(maid, plan.Axis, _content.Tuning),

            NightProgramKind.Care =>
                NightConversionRule.RunCare(maid, plan.Axis, _content.Tuning),

            _ => NightProgramResult.Failed(plan.Kind, maid.MaidId, plan.Axis, "지정되지 않은 처리"),
        };
    }

    // 경험치가 기준을 넘긴 트랙은 자동으로 레벨이 오르지 않는다.
    // 여기서 이벤트를 실제로 소화해야 레벨업이 확정된다.
    private async YarnTask RunMasteryEventsAsync(CampaignState campaign)
    {
        IReadOnlyList<MaidRuntimeState> maids = campaign.Maids;

        for (int i = 0; i < maids.Count; i++)
        {
            MaidRuntimeState maid = maids[i];

            if (maid.IsLost)
                continue;

            while (maid.TryFindReadyMasteryAxis(_content.Tuning, out BurdenAxis axis))
            {
                int levelAfter = maid.GetMastery(axis).Level + 1;

                MasteryEventResult result = NightConversionRule.RunMasteryEvent(
                    maid,
                    axis,
                    _content.Tuning,
                    GuesthouseNodeNaming.MasteryEvent(maid.MaidId, axis, levelAfter));

                if (!result.IsLevelUpCommitted)
                    break;

                await _presentation.PlayMasteryEventAsync(result);
            }
        }
    }
}
