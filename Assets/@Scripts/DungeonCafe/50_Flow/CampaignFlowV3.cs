using System;
using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 캠페인 전체 플로우 v3: 15일 × (낮 접객 → 밤 처리). (§1, §5~§6, §15)
/// 기존 CampaignFlow / DayCycleFlow / NightPhaseFlow 를 대체한다.
/// </summary>
public sealed class CampaignFlowV3
{
    private readonly CampaignStateV3 _campaign;
    private readonly IGuesthouseV3Screens _screens;
    private readonly INodePlayerV3 _nodes;
    private readonly ServiceSessionFlowV3 _sessionFlow;

    private GuesthouseTuningV3 Tuning => _campaign.Tuning;
    private GuesthouseV3ContentDB Content => _campaign.Content;

    public CampaignFlowV3(CampaignStateV3 campaign, IGuesthouseV3Screens screens, INodePlayerV3 nodes)
    {
        _campaign = campaign; _screens = screens; _nodes = nodes;
        _sessionFlow = new ServiceSessionFlowV3(campaign, screens, nodes);
    }

    public async YarnTask<EndingKindV3> RunAsync()
    {
        int dayCount = Content.CampaignDayCount;

        for (int day = _campaign.CurrentDayNumber; day <= dayCount; day++)
        {
            _campaign.CurrentDayNumber = day;

            DayStateV3 dayState = await RunDayAsync(day);

            if (CheckImmediateEnding()) break;

            await RunNightAsync(dayState);

            if (CheckImmediateEnding()) break;

            _campaign.Ledger.StartNewDay();
            _campaign.Abilities.StartNewDay();
        }

        if (_campaign.Ending == EndingKindV3.None)
            _campaign.Ending = EndingResolverV3.ResolveCampaignEnd(_campaign);

        _campaign.Phase = CampaignPhaseV3.Finished;
        await _screens.PresentEndingAsync(_campaign, _campaign.Ending);
        return _campaign.Ending;
    }

    private bool CheckImmediateEnding()
    {
        EndingKindV3 immediate = EndingResolverV3.ResolveImmediate(_campaign);
        if (immediate == EndingKindV3.None) return false;
        _campaign.Ending = immediate;
        return true;
    }

    // ------------------------------------------------------------
    // 낮 (§1, §2)
    // ------------------------------------------------------------
    private async YarnTask<DayStateV3> RunDayAsync(int day)
    {
        CampaignDayPlan plan = Content.GetDayPlan(day);
        var dayState = new DayStateV3(day, plan);
        _campaign.Phase = CampaignPhaseV3.SlotBoundary;

        // 예약 편성: 결정론 회전. 신규 등장 개체는 등장일에 반드시 포함. (§1)
        List<MonsterProfileV3> pool = Content.GetMonsterPool(day);
        var bookings = new List<MonsterProfileV3>(plan.ServiceSlots);

        MonsterProfileV3 debutant = null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i].AppearDay == day) { debutant = pool[i]; break; }

        for (int slot = 0; slot < plan.ServiceSlots; slot++)
        {
            MonsterProfileV3 pick = slot == 0 && debutant != null
                ? debutant
                : pool[(day * 3 + slot * 5) % pool.Count];

            if (bookings.Contains(pick) && pool.Count > plan.ServiceSlots)
                pick = pool[(day * 3 + slot * 5 + 1) % pool.Count];

            bookings.Add(pick);
            dayState.BookedMonsterIds.Add(pick.MonsterId);
        }

        await _screens.PresentBoardAsync(day, bookings, _campaign);

        // 예약 확정 통화: 첫 통화 개체는 노드 재생 + 이해도. (§8.2)
        for (int i = 0; i < bookings.Count; i++)
        {
            MonsterProfileV3 monster = bookings[i];
            if (_campaign.Understanding.MarkPhoneCalled(monster.MonsterId))
            {
                await _nodes.PlayNodeAsync(monster.PhoneCallNodeName);
                _campaign.Understanding.AddPoints(monster.MonsterId, Tuning.UnderstandingPerPhoneCall);
            }
        }

        // 슬롯 루프
        for (int slot = 0; slot < bookings.Count; slot++)
        {
            _campaign.Phase = CampaignPhaseV3.SlotBoundary;

            List<MaidStateV3> candidates = _campaign.GetAssignable(day);
            if (candidates.Count == 0)
            {
                dayState.CompletedSlots++;
                continue;   // 전원 배정 불가 → 슬롯 유실 (수입 0)
            }

            string maidId = await _screens.RequestAssignmentAsync(bookings[slot], candidates, _campaign);
            MaidStateV3 maid = _campaign.GetMaid(maidId) ?? candidates[0];
            if (!maid.CanBeAssigned(day)) maid = candidates[0];

            await _sessionFlow.RunAsync(maid, bookings[slot]);
            dayState.CompletedSlots++;

            if (EndingResolverV3.ResolveImmediate(_campaign) != EndingKindV3.None)
                break;
        }

        // 하루 리포트 + 할당 판정 (§7.4)
        bool quotaMet = _campaign.Ledger.MeetsQuota(plan.Quota);
        if (!quotaMet)
        {
            _campaign.BankruptcyCount++;
            await _nodes.PlayNodeAsync($"Quota_Warning_{_campaign.BankruptcyCount}");
        }

        await _screens.PresentDayReportAsync(_campaign, dayState, quotaMet);
        return dayState;
    }

    // ------------------------------------------------------------
    // 밤 (§5, §6)
    // ------------------------------------------------------------
    private async YarnTask RunNightAsync(DayStateV3 dayState)
    {
        _campaign.Phase = CampaignPhaseV3.NightStart;

        await RunNightPrepAsync();

        _campaign.Phase = CampaignPhaseV3.InNight;

        int day = dayState.DayNumber;
        var present = new List<MaidStateV3>();
        for (int i = 0; i < _campaign.Maids.Count; i++)
        {
            MaidStateV3 m = _campaign.Maids[i];
            if (!m.IsLost && day >= m.Profile.UnlockDay) present.Add(m);
        }

        int manageCount = Tuning.GetNightManageCount(_campaign.ShopLevel);

        var quirkRequests = new List<(string, string)>(_campaign.PendingQuirkRequests);
        _campaign.PendingQuirkRequests.Clear();

        IReadOnlyList<NightChoiceV3> choices = await _screens.RequestNightPlanAsync(
            new NightPlanRequestV3(day, manageCount, present, Tuning, quirkRequests));

        var caredIds = new HashSet<string>(StringComparer.Ordinal);
        var managedIds = new HashSet<string>(StringComparer.Ordinal);
        int used = 0;

        if (choices != null)
        {
            foreach (NightChoiceV3 choice in choices)
            {
                if (used >= manageCount) break;
                MaidStateV3 maid = _campaign.GetMaid(choice.MaidId);
                if (maid == null || maid.IsLost || managedIds.Contains(maid.MaidId) || caredIds.Contains(maid.MaidId))
                    continue;

                if (choice.Kind == NightChoiceKind.Care)
                {
                    await RunCareAsync(maid);
                    caredIds.Add(maid.MaidId);
                    used++;
                }
                else if (choice.Kind == NightChoiceKind.ManagedRelease)
                {
                    if (await TryRunReleaseAsync(maid))
                    {
                        managedIds.Add(maid.MaidId);
                        used++;
                    }
                }
            }
        }

        // ---- 방치 일괄 (§6.2). 순서 고정 = 캠페인 메이드 순. ----
        var neglectDice = new CommittingDice(_campaign, "neglect");

        for (int i = 0; i < present.Count; i++)
        {
            MaidStateV3 maid = present[i];
            bool handled = caredIds.Contains(maid.MaidId) || managedIds.Contains(maid.MaidId);

            if (!handled)
                await RunNeglectAsync(maid, dayState, neglectDice);

            // 공동: 안정받지 못한 밤마다 관계 −1. (§9)
            if (!caredIds.Contains(maid.MaidId))
            {
                for (int a = 0; a < maid.Aftereffects.Count; a++)
                    if (maid.Aftereffects[a].Definition.PenalizesRelationWhenNeglected)
                        maid.AddRelation(-Tuning.HollowNightlyRelationPenalty, RelationDirection.Trust);
            }
        }

        // 후유증 하루 경과 (방치·처리 무관, 안정 해소분은 이미 제거됨)
        for (int i = 0; i < present.Count; i++)
            AdvanceAftereffects(present[i], caredIds.Contains(present[i].MaidId));

        // 메이드 간 대화
        await _nodes.PlayNodeAsync($"Night_Talk_{day}");

        // 수첩 분석 (§8 Lv2)
        int analyses = Tuning.GetAnalysisCount(_campaign.ShopLevel);
        for (int i = 0; i < analyses && i < dayState.BookedMonsterIds.Count; i++)
            UnderstandingRule.GrantAnalysis(_campaign, LeastUnderstood(dayState.BookedMonsterIds));

        _campaign.Phase = CampaignPhaseV3.DayEnd;
    }

    private async YarnTask RunNightPrepAsync()
    {
        var purchasable = new List<PlayerAbilityDefinition>();
        for (int i = 0; i < Content.Abilities.Count; i++)
        {
            PlayerAbilityDefinition def = Content.Abilities[i];
            if (_campaign.Abilities.Owns(def.Id)) continue;
            if (def.OwnerMaidId != null) { TryGrantRelationAbility(def); continue; }   // 전용은 관계로 자동 습득. (§11.3)
            if (AbilityRules.MeetsConditions(_campaign, def)) purchasable.Add(def);
        }

        int slotLimit = Tuning.GetAbilitySlots(_campaign.ShopLevel);

        NightPrepResponseV3 response = await _screens.RequestNightPrepAsync(new NightPrepRequestV3(
            purchasable, new List<string>(_campaign.Abilities.Owned),
            _campaign.Abilities.Equipped, slotLimit, _campaign.Ledger.Held));

        if (response.PurchaseIds != null)
            foreach (string id in response.PurchaseIds)
            {
                PlayerAbilityDefinition def = Content.GetAbility(id);
                if (def != null) AbilityRules.TryPurchase(_campaign, def);
            }

        if (response.EquipIds != null)
        {
            var current = new List<string>(_campaign.Abilities.Equipped);
            for (int i = 0; i < current.Count; i++) _campaign.Abilities.Unequip(current[i]);
            foreach (string id in response.EquipIds) _campaign.Abilities.Equip(id, slotLimit);
        }
    }

    private void TryGrantRelationAbility(PlayerAbilityDefinition def)
    {
        if (AbilityRules.MeetsConditions(_campaign, def))
            _campaign.Abilities.Grant(def.Id);
    }

    private async YarnTask RunCareAsync(MaidStateV3 maid)
    {
        int reduction = Tuning.GetCareReduction(_campaign.ShopLevel)
                        + QuirkEffectResolver.CareReductionDelta(Content, maid);   // 공동의 흔적 −5

        BurdenAxis axis = maid.Gauge.HighestAxis(out _);
        maid.Gauge.Reduce(axis, Math.Max(0, reduction));

        // 후유증 1단계 해제. (§6.1)
        if (maid.Aftereffects.Count > 0)
        {
            AftereffectInstance first = maid.Aftereffects[0];
            if (first.ApplyCare()) maid.RemoveAftereffect(first);
        }

        maid.AddRelation(Tuning.RelationPointsCare, RelationDirection.Trust);

        int tier = RelationRule.ResolveStage(maid.RelationPoints, Tuning);
        await _nodes.PlayNodeAsync($"Night_Care_{maid.MaidId}_{tier}");
    }

    private async YarnTask<bool> TryRunReleaseAsync(MaidStateV3 maid)
    {
        BurdenAxis axis = maid.Gauge.HighestAxis(out int entry);
        if (entry < Tuning.ManagedReleaseMinimumCollapse || entry >= Tuning.ControlLossThreshold)
            return false;                                                       // §6.1: 80~99 한정

        // 100까지 끌어올린 뒤 진입 시점의 retain% 로 회수. (§6.1)
        maid.Gauge.SetValue(axis, Tuning.ControlLossThreshold);
        int retain = QuirkEffectResolver.ManagedRetainPercent(Content, maid, Tuning);
        maid.Gauge.SetValue(axis, entry * retain / 100);

        _campaign.Ledger.EarnNight(Tuning.ManagedReleaseNightEnergy);

        maid.GetMastery(axis).AddExperience(Tuning.ManagedReleaseMasteryExperience);
        if (maid.GetMastery(axis).CommitLevelUp(Tuning))
            await _nodes.PlayNodeAsync($"Mastery_{maid.MaidId}_{axis}_{maid.GetMastery(axis).Level}");

        int relationBonus = QuirkEffectResolver.ReleaseRelationBonus(Content, maid);
        maid.AddRelation(Tuning.RelationPointsRelease + relationBonus, RelationDirection.Depend);

        // 안정 기벽 획득 판정: 관계 2단계↑에서 60%, 소유 기벽 순차. (§10.1)
        int stage = RelationRule.ResolveStage(maid.RelationPoints, Tuning);
        if (stage >= 2 && _campaign.CommitPercent("stablequirk", 60))
            GrantNextStableQuirk(maid);

        await _nodes.PlayNodeAsync($"Night_Release_{maid.MaidId}_{stage}");
        return true;
    }

    private void GrantNextStableQuirk(MaidStateV3 maid)
    {
        for (int i = 0; i < Content.Quirks.Count; i++)
        {
            QuirkDefinition q = Content.Quirks[i];
            if (q.IsAccident || q.OwnerMaidId != maid.MaidId) continue;
            if (maid.HasQuirkId(q.Id)) continue;
            maid.AddQuirk(q.Id, isAccident: false);
            return;
        }
    }

    private async YarnTask RunNeglectAsync(MaidStateV3 maid, DayStateV3 dayState, CommittingDice dice)
    {
        maid.Gauge.HighestAxis(out int highest);

        NeglectRule.NeglectChances chances = QuirkEffectResolver.NeglectChances(Content, maid, Tuning);
        NeglectJudgment judgment = NeglectRule.Judge(
            dice, highest, maid.HasAftereffect, maid.HasQuirk, chances, Tuning);

        await _screens.PresentNeglectAsync(maid, judgment);

        switch (judgment.Outcome)
        {
            case NeglectCollapseOutcome.NaturalRecovery:
            {
                int recovery = Tuning.NeglectNaturalRecovery
                               + QuirkEffectResolver.NeglectRecoveryBonus(Content, maid);   // 곁잠 +6
                BurdenAxis axis = maid.Gauge.HighestAxis(out _);
                maid.Gauge.Reduce(axis, recovery);

                // 숨겨진 성향 자율행동. (§12.1)
                if (_campaign.CommitPercent("disposition", maid.Profile.DispositionChancePercent))
                    await RunDispositionAsync(maid, dayState);
                break;
            }
            case NeglectCollapseOutcome.SelfRelease:
            {
                BurdenAxis axis = maid.Gauge.HighestAxis(out _);
                maid.Gauge.SetValue(axis, Math.Min(highest, judgment.CollapseAfter));
                if (judgment.GainsAccidentQuirk) GrantNextAccidentQuirk(maid);
                await _nodes.PlayNodeAsync($"Night_Auto_{maid.MaidId}_selfrelease");
                break;
            }
            case NeglectCollapseOutcome.NightIncident:
            {
                await _nodes.PlayNodeAsync($"Night_Incident_Omen_{maid.MaidId}");    // 전조 필수. (§6.2)
                await RunNightIncidentAsync(maid, dayState);
                break;
            }
        }

        if (judgment.SchedulesQuirkRequest && maid.QuirkIds.Count > 0)
            _campaign.PendingQuirkRequests.Add((maid.MaidId, maid.QuirkIds[maid.QuirkIds.Count - 1]));
    }

    private async YarnTask RunDispositionAsync(MaidStateV3 maid, DayStateV3 dayState)
    {
        switch (maid.Profile.DispositionKey)
        {
            case "training":    // 시온 야간 단련: 육체 −8 추가, 5% 부상 +5. (§12.1)
                maid.Gauge.Reduce(BurdenAxis.Physical, 8);
                if (_campaign.CommitPercent("disp_injury", 5))
                    maid.Gauge.Add(BurdenAxis.Physical, 5);
                break;
            case "archiving":   // 아리에 기록 정리: 이해도 +1, 정신 +4.
                if (dayState.BookedMonsterIds.Count > 0)
                    UnderstandingRule.GrantAnalysis(_campaign, dayState.BookedMonsterIds[0]);
                maid.Gauge.Add(BurdenAxis.Mental, 4);
                break;
            case "greeting":    // 루이 몰래 마중: 감응 ±10 (50/50), 성공 시 관계 +1.
                if (_campaign.CommitPercent("disp_greet", 50))
                {
                    maid.Gauge.Reduce(BurdenAxis.Empathic, 10);
                    maid.AddRelation(Tuning.RelationPointsAutoEvent, RelationDirection.Trust);
                }
                else
                {
                    maid.Gauge.Add(BurdenAxis.Empathic, 10);
                }
                break;
        }

        await _nodes.PlayNodeAsync($"Night_Auto_{maid.MaidId}_{maid.Profile.DispositionKey}");
    }

    private async YarnTask RunNightIncidentAsync(MaidStateV3 maid, DayStateV3 dayState)
    {
        // 심야 사건: 오늘 접객한 개체(없으면 임의)와의 자동 심층 2비트. (§6.2)
        MonsterProfileV3 monster = null;
        if (dayState.BookedMonsterIds.Count > 0)
            monster = Content.GetMonster(dayState.BookedMonsterIds[dayState.BookedMonsterIds.Count - 1]);
        monster ??= Content.Monsters[0];

        var session = new ServiceSessionStateV3(maid, monster, Content.GetProtocol(monster.Species))
        {
            InDepth = true,
        };
        session.DepthAxis = maid.Gauge.HighestAxis(out _);

        await _sessionFlow.RunDepthAsync(session, playerControlled: false,
            maxBeats: Tuning.NightIncidentDepthBeats);
    }

    private void GrantNextAccidentQuirk(MaidStateV3 maid)
    {
        for (int i = 0; i < Content.Quirks.Count; i++)
        {
            QuirkDefinition q = Content.Quirks[i];
            if (!q.IsAccident || q.EffectKind == QuirkEffectKind.HollowMark) continue;
            if (maid.HasQuirkId(q.Id)) continue;
            maid.AddQuirk(q.Id, isAccident: true);
            return;
        }
    }

    private void AdvanceAftereffects(MaidStateV3 maid, bool caredTonight)
    {
        if (maid.Aftereffects.Count == 0) return;

        var snapshot = new List<AftereffectInstance>(maid.Aftereffects);
        for (int i = 0; i < snapshot.Count; i++)
        {
            AftereffectInstance instance = snapshot[i];
            if (caredTonight && i == 0) continue;   // 오늘 안정으로 처리된 첫 항목은 경과 제외

            bool healed = instance.AdvanceNight(Tuning, out bool permanentize);

            if (permanentize)
            {
                maid.RemoveAftereffect(instance);
                maid.AddQuirk(instance.Definition.PermanentizeQuirkId, isAccident: true);   // 각인 → 각인 잔향. (§9)
            }
            else if (healed)
            {
                maid.RemoveAftereffect(instance);
            }
        }
    }

    private string LeastUnderstood(List<string> monsterIds)
    {
        string best = monsterIds[0];
        int bestPoints = int.MaxValue;
        for (int i = 0; i < monsterIds.Count; i++)
        {
            int p = _campaign.Understanding.GetPoints(monsterIds[i]);
            if (p < bestPoints) { bestPoints = p; best = monsterIds[i]; }
        }
        return best;
    }
}
