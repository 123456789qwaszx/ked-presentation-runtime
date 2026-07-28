using System;
using System.Collections.Generic;
using Yarn.Unity;

// 밤 진행.
// 상점과 장착 -> 처리 계획 -> 선택받지 못한 메이드 방치
// -> 후유증 경과 -> 메이드 대화 -> 수첩 분석.
// 이 클래스는 밤 전체의 순서와 대기만 담당한다.
public sealed class NightPhaseFlowV3
{
    private readonly NightPrepFlowV3 _prepFlow;
    private readonly NightMaidFlowV3 _maidFlow;

    private readonly VnScreenBindings _screens;
    private readonly INodePlayerV3 _nodes;

    public NightPhaseFlowV3(
        NightPrepFlowV3 prepFlow,
        NightMaidFlowV3 maidFlow,
        VnScreenBindings screens,
        INodePlayerV3 nodes)
    {
        _prepFlow = prepFlow;
        _maidFlow = maidFlow;
        _screens = screens;
        _nodes = nodes;
    }

    public async YarnTask RunNightAsync(
        CampaignStateV3 campaign,
        DayStateV3 dayState)
    {
        campaign.Phase = CampaignPhaseV3.NightStart;

        await _prepFlow.RunAsync(campaign);

        campaign.Phase = CampaignPhaseV3.InNight;

        List<MaidStateV3> present =
            CollectPresentMaids(campaign, dayState.DayNumber);

        IReadOnlyList<NightChoiceV3> choices =
            await RequestNightPlanAsync(campaign, dayState, present);

        var caredIds =
            new HashSet<string>(StringComparer.Ordinal);

        var releasedIds =
            new HashSet<string>(StringComparer.Ordinal);

        await RunChosenProgramsAsync(
            campaign,
            choices,
            caredIds,
            releasedIds);

        await RunNeglectedMaidsAsync(
            campaign,
            dayState,
            present,
            caredIds,
            releasedIds);

        AdvanceAftereffects(
            campaign,
            present,
            caredIds);

        await RunClosingEventsAsync(
            campaign,
            dayState);

        campaign.Phase = CampaignPhaseV3.DayEnd;
    }

    private List<MaidStateV3> CollectPresentMaids(
        CampaignStateV3 campaign,
        int dayNumber)
    {
        var result = new List<MaidStateV3>();

        for (int i = 0; i < campaign.Maids.Count; i++)
        {
            MaidStateV3 maid = campaign.Maids[i];

            if (!maid.IsLost
                && dayNumber >= maid.Profile.UnlockDay)
            {
                result.Add(maid);
            }
        }

        return result;
    }

    private async YarnTask<IReadOnlyList<NightChoiceV3>>
        RequestNightPlanAsync(
            CampaignStateV3 campaign,
            DayStateV3 dayState,
            IReadOnlyList<MaidStateV3> present)
    {
        int manageCount =
            campaign.Tuning.GetNightManageCount(
                campaign.ShopLevel);

        var quirkRequests =
            new List<(string, string)>(
                campaign.PendingQuirkRequests);

        campaign.PendingQuirkRequests.Clear();

        NightPlanRequestV3 request = new(
            dayState.DayNumber,
            manageCount,
            present,
            campaign.Tuning,
            quirkRequests);

        return await _screens.RequestNightPlanAsync(request);
    }

    private async YarnTask RunChosenProgramsAsync(
        CampaignStateV3 campaign,
        IReadOnlyList<NightChoiceV3> choices,
        HashSet<string> caredIds,
        HashSet<string> releasedIds)
    {
        if (choices == null)
            return;

        int manageCount =
            campaign.Tuning.GetNightManageCount(
                campaign.ShopLevel);

        int used = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            if (used >= manageCount)
                break;

            NightChoiceV3 choice = choices[i];
            MaidStateV3 maid =
                campaign.GetMaid(choice.MaidId);

            if (!CanRunChoice(
                maid,
                caredIds,
                releasedIds))
            {
                continue;
            }

            if (choice.Kind == NightChoiceKind.Care)
            {
                await _maidFlow.RunCareAsync(
                    campaign,
                    maid);

                caredIds.Add(maid.MaidId);
                used++;
                continue;
            }

            if (choice.Kind == NightChoiceKind.ManagedRelease
                && await _maidFlow.TryRunManagedReleaseAsync(
                    campaign,
                    maid))
            {
                releasedIds.Add(maid.MaidId);
                used++;
            }
        }
    }

    private static bool CanRunChoice(
        MaidStateV3 maid,
        HashSet<string> caredIds,
        HashSet<string> releasedIds)
    {
        return maid != null
            && !maid.IsLost
            && !caredIds.Contains(maid.MaidId)
            && !releasedIds.Contains(maid.MaidId);
    }

    private async YarnTask RunNeglectedMaidsAsync(
        CampaignStateV3 campaign,
        DayStateV3 dayState,
        IReadOnlyList<MaidStateV3> present,
        HashSet<string> caredIds,
        HashSet<string> releasedIds)
    {
        // 메이드 순서가 곧 방치 판정의 난수 소비 순서다.
        var dice = new CommittingDice(
            campaign,
            "neglect");

        for (int i = 0; i < present.Count; i++)
        {
            MaidStateV3 maid = present[i];

            bool handled =
                caredIds.Contains(maid.MaidId)
                || releasedIds.Contains(maid.MaidId);

            if (!handled)
            {
                await _maidFlow.RunNeglectAsync(
                    campaign,
                    maid,
                    dayState,
                    dice);
            }

            if (!caredIds.Contains(maid.MaidId))
            {
                _maidFlow.ApplyNeglectedRelationPenalty(
                    campaign,
                    maid);
            }
        }
    }

    private void AdvanceAftereffects(
        CampaignStateV3 campaign,
        IReadOnlyList<MaidStateV3> present,
        HashSet<string> caredIds)
    {
        for (int i = 0; i < present.Count; i++)
        {
            MaidStateV3 maid = present[i];

            _maidFlow.AdvanceAftereffects(
                campaign,
                maid,
                caredIds.Contains(maid.MaidId));
        }
    }

    private async YarnTask RunClosingEventsAsync(
        CampaignStateV3 campaign,
        DayStateV3 dayState)
    {
        await _nodes.PlayNodeAsync(
            $"Night_Talk_{dayState.DayNumber}");

        int analysisCount =
            campaign.Tuning.GetAnalysisCount(
                campaign.ShopLevel);

        for (int i = 0;
             i < analysisCount
             && i < dayState.Bookings.Count;
             i++)
        {
            UnderstandingRule.GrantAnalysis(
                campaign,
                FindLeastUnderstood(
                    campaign,
                    dayState.Bookings));
        }
    }

    private static string FindLeastUnderstood(
        CampaignStateV3 campaign,
        IReadOnlyList<MonsterProfileV3> bookings)
    {
        string selectedMonsterId =
            bookings[0].MonsterId;

        int selectedPoints =
            int.MaxValue;

        for (int i = 0; i < bookings.Count; i++)
        {
            string monsterId =
                bookings[i].MonsterId;

            int points =
                campaign.Understanding.GetPoints(
                    monsterId);

            if (points < selectedPoints)
            {
                selectedMonsterId = monsterId;
                selectedPoints = points;
            }
        }

        return selectedMonsterId;
    }
}
