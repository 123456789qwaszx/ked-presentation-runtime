using System;
using System.Collections.Generic;
using Yarn.Unity;

// 밤에 메이드 한 명에게 일어나는 처리를 담당한다.
// 안정/관리 붕괴/방치/자율행동/심야 사건과 후유증 경과가 이 클래스에 모인다.
public sealed class NightMaidFlowV3
{
    private readonly GuesthouseV3ContentDB _content;
    private readonly ServiceSessionFlowV3 _sessionFlow;

    private readonly VnScreenBindings _screens;
    private readonly INodePlayerV3 _nodes;

    public NightMaidFlowV3(
        GuesthouseV3ContentDB content,
        ServiceSessionFlowV3 sessionFlow,
        VnScreenBindings screens,
        INodePlayerV3 nodes)
    {
        _content = content;
        _sessionFlow = sessionFlow;
        _screens = screens;
        _nodes = nodes;
    }

    public async YarnTask RunCareAsync(
        CampaignStateV3 campaign,
        MaidStateV3 maid)
    {
        GuesthouseTuningV3 tuning = campaign.Tuning;

        int reduction =
            tuning.GetCareReduction(campaign.ShopLevel)
            + QuirkEffectResolver.CareReductionDelta(_content, maid);

        reduction = Math.Max(0, reduction);

        // 케어는 전 축을 동시에 조금씩 낮춘다.
        for (int i = 0; i < BurdenAxes.Count; i++)
            maid.Gauge.Reduce(BurdenAxes.FromIndex(i), reduction);

        maid.AddRelation(
            tuning.RelationPointsCare,
            RelationDirection.Trust);

        int relationStage =
            RelationRule.ResolveStage(maid.RelationPoints, tuning);

        await _nodes.PlayNodeAsync(
            $"Night_Care_{maid.MaidId}_{relationStage}");
    }

    public async YarnTask<bool> TryRunManagedReleaseAsync(
        CampaignStateV3 campaign,
        MaidStateV3 maid)
    {
        if (maid.FindAftereffect("se_tremor") != null)
            return false;
        
        GuesthouseTuningV3 tuning = campaign.Tuning;

        BurdenAxis axis = maid.Gauge.HighestAxis(out int entry);

        if (entry < tuning.ManagedReleaseMinimumCollapse
            || entry >= tuning.ControlLossThreshold)
        {
            return false;
        }

        // 한 축을 완전히 비운다.
        maid.Gauge.SetValue(axis, 0);

        campaign.Ledger.EarnNight(
            tuning.ManagedReleaseNightEnergy);

        maid.GetMastery(axis).AddExperience(
            tuning.ManagedReleaseMasteryExperience);

        if (maid.GetMastery(axis).CommitLevelUp(tuning))
        {
            await _nodes.PlayNodeAsync(
                $"Mastery_{maid.MaidId}_{axis}_{maid.GetMastery(axis).Level}");
        }

        int relationBonus =
            QuirkEffectResolver.ReleaseRelationBonus(_content, maid);

        maid.AddRelation(
            tuning.RelationPointsRelease + relationBonus,
            RelationDirection.Depend);

        int relationStage =
            RelationRule.ResolveStage(maid.RelationPoints, tuning);

        if (relationStage >= 2
            && campaign.CommitPercent("stablequirk", 60))
        {
            GrantNextStableQuirk(maid);
        }

        await _nodes.PlayNodeAsync(
            $"Night_Release_{maid.MaidId}_{relationStage}");
        
        //관리 붕괴로 각인 해소
        maid.RemoveAftereffect(maid.FindAftereffect("se_brand"));

        return true;
    }

    public async YarnTask RunNeglectAsync(
        CampaignStateV3 campaign,
        MaidStateV3 maid,
        DayStateV3 dayState,
        CommittingDice dice)
    {
        GuesthouseTuningV3 tuning = campaign.Tuning;

        maid.Gauge.HighestAxis(out int highest);

        NeglectRule.NeglectChances chances =
            QuirkEffectResolver.NeglectChances(
                _content,
                maid,
                tuning);

        NeglectJudgment judgment = NeglectRule.Judge(
            dice,
            highest,
            maid.HasAftereffect,
            maid.HasQuirk,
            chances,
            tuning);

        await _screens.PresentNeglectAsync(maid, judgment);

        switch (judgment.Outcome)
        {
            case NeglectCollapseOutcome.NaturalRecovery:
                await RunNaturalRecoveryAsync(
                    campaign,
                    maid,
                    dayState);
                break;

            case NeglectCollapseOutcome.SelfRelease:
                await RunSelfReleaseAsync(
                    maid,
                    highest,
                    judgment);
                break;

            case NeglectCollapseOutcome.NightIncident:
                await RunNightIncidentAsync(
                    campaign,
                    maid,
                    dayState);
                break;
        }

        if (judgment.SchedulesQuirkRequest
            && maid.QuirkIds.Count > 0)
        {
            campaign.PendingQuirkRequests.Add((
                maid.MaidId,
                maid.QuirkIds[maid.QuirkIds.Count - 1]));
        }
    }

    public void AdvanceAftereffects(CampaignStateV3 campaign, MaidStateV3 maid)
    {
        if (maid.Aftereffects.Count == 0)
            return;

        maid.Gauge.HighestAxis(out int highest);
        if (highest != 0)
            return;

        var snapshot = new List<AftereffectInstance>(maid.Aftereffects);
        for (int i = 0; i < snapshot.Count; i++)
            maid.RemoveAftereffect(snapshot[i]);
    }

    private async YarnTask RunNaturalRecoveryAsync(
        CampaignStateV3 campaign,
        MaidStateV3 maid,
        DayStateV3 dayState)
    {
        int recovery =
            campaign.Tuning.NeglectNaturalRecovery
            + QuirkEffectResolver.NeglectRecoveryBonus(
                _content,
                maid);

        BurdenAxis axis = maid.Gauge.HighestAxis(out _);
        maid.Gauge.Reduce(axis, recovery);

        if (campaign.CommitPercent(
            "disposition",
            maid.Profile.DispositionChancePercent))
        {
            await RunDispositionAsync(
                campaign,
                maid,
                dayState);
        }
    }

    private async YarnTask RunSelfReleaseAsync(
        MaidStateV3 maid,
        int highest,
        NeglectJudgment judgment)
    {
        BurdenAxis axis = maid.Gauge.HighestAxis(out _);

        maid.Gauge.SetValue(
            axis,
            Math.Min(highest, judgment.CollapseAfter));

        if (judgment.GainsAccidentQuirk)
            GrantNextAccidentQuirk(maid);

        await _nodes.PlayNodeAsync(
            $"Night_Auto_{maid.MaidId}_selfrelease");
    }

    private async YarnTask RunDispositionAsync(
        CampaignStateV3 campaign,
        MaidStateV3 maid,
        DayStateV3 dayState)
    {
        switch (maid.Profile.DispositionKey)
        {
            // 시온: 육체 −8, 5%로 부상 +5.
            case "training":
                maid.Gauge.Reduce(BurdenAxis.Physical, 8);

                if (campaign.CommitPercent("disp_injury", 5))
                    maid.Gauge.Add(BurdenAxis.Physical, 5);
                break;

            // 아리에: 오늘 개체 이해도 +1, 정신 +4.
            case "archiving":
                if (dayState.Bookings.Count > 0)
                {
                    UnderstandingRule.GrantAnalysis(
                        campaign,
                        dayState.Bookings[0].MonsterId);
                }

                maid.Gauge.Add(BurdenAxis.Mental, 4);
                break;

            // 루이: 감응 ±10. 성공 시 관계 +1.
            case "greeting":
                if (campaign.CommitPercent("disp_greet", 50))
                {
                    maid.Gauge.Reduce(BurdenAxis.Empathic, 10);
                    maid.AddRelation(
                        campaign.Tuning.RelationPointsAutoEvent,
                        RelationDirection.Trust);
                }
                else
                {
                    maid.Gauge.Add(BurdenAxis.Empathic, 10);
                }
                break;
        }

        await _nodes.PlayNodeAsync(
            $"Night_Auto_{maid.MaidId}_{maid.Profile.DispositionKey}");
    }

    private async YarnTask RunNightIncidentAsync(
        CampaignStateV3 campaign,
        MaidStateV3 maid,
        DayStateV3 dayState)
    {
        await _nodes.PlayNodeAsync(
            $"Night_Incident_Omen_{maid.MaidId}");

        MonsterProfileV3 monster =
            ResolveNightIncidentMonster(dayState);

        var session = new ServiceSessionStateV3(
            maid,
            monster,
            _content.GetProtocol(monster.Species))
        {
            InDepth = true,
        };

        session.DepthAxis =
            maid.Gauge.HighestAxis(out _);

        await _sessionFlow.RunDepthAsync(
            session,
            playerControlled: false,
            maxBeats: campaign.Tuning.NightIncidentDepthBeats);
    }

    private MonsterProfileV3 ResolveNightIncidentMonster(
        DayStateV3 dayState)
    {
        if (dayState.Bookings.Count > 0)
        {
            string monsterId =
                dayState.Bookings[
                    dayState.Bookings.Count - 1].MonsterId;

            MonsterProfileV3 booked =
                _content.GetMonster(monsterId);

            if (booked != null)
                return booked;
        }

        return _content.Monsters[0];
    }

    private void GrantNextStableQuirk(MaidStateV3 maid)
    {
        for (int i = 0; i < _content.Quirks.Count; i++)
        {
            QuirkDefinition quirk = _content.Quirks[i];

            if (quirk.IsAccident
                || quirk.OwnerMaidId != maid.MaidId
                || maid.HasQuirkId(quirk.Id))
            {
                continue;
            }

            maid.AddQuirk(quirk.Id, isAccident: false);
            return;
        }
    }

    private void GrantNextAccidentQuirk(MaidStateV3 maid)
    {
        for (int i = 0; i < _content.Quirks.Count; i++)
        {
            QuirkDefinition quirk = _content.Quirks[i];
            
            // 죽음의 낙인은 완전붕괴에서만 부여
            if (!quirk.IsAccident
                || quirk.Id == "qk_acc_hollowmark"
                || maid.HasQuirkId(quirk.Id))
                continue;

            maid.AddQuirk(quirk.Id, isAccident: true);
            return;
        }
    }
}
