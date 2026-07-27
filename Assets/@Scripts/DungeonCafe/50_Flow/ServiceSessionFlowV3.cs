using System;
using System.Collections.Generic;
using Yarn.Unity;

/// <summary>캠페인 상태의 커밋 창구를 IDiceSource 로 감싸, 규칙에 흘려 넣는다. (§14)</summary>
public sealed class CommittingDice : IDiceSource
{
    private readonly CampaignStateV3 _campaign;
    private readonly string _kind;
    public CommittingDice(CampaignStateV3 campaign, string kind) { _campaign = campaign; _kind = kind; }
    public int NextInclusive(int min, int max) => _campaign.CommitRoll(_kind, min, max);
}

/// <summary>
/// 접객 세션 플로우 v3. (§2~§5)
/// 기존 ServiceSessionFlow + ServiceSessionFlow_Autonomous 를 대체한다.
/// 낮 비트 루프 → (100 도달 시) 붕괴심층 루프 → 결산까지가 한 세션이다.
/// </summary>
public sealed class ServiceSessionFlowV3
{
    private readonly CampaignStateV3 _campaign;
    private readonly IGuesthouseV3Screens _screens;
    private readonly INodePlayerV3 _nodes;

    private GuesthouseTuningV3 Tuning => _campaign.Tuning;
    private GuesthouseV3ContentDB Content => _campaign.Content;

    public ServiceSessionFlowV3(CampaignStateV3 campaign, IGuesthouseV3Screens screens, INodePlayerV3 nodes)
    {
        _campaign = campaign; _screens = screens; _nodes = nodes;
    }

    public async YarnTask<SettlementV3Result> RunAsync(MaidStateV3 maid, MonsterProfileV3 monster)
    {
        _campaign.Phase = CampaignPhaseV3.InService;
        _campaign.Abilities.StartNewService();

        var session = new ServiceSessionStateV3(maid, monster, Content.GetProtocol(monster.Species));

        int beatCount = monster.Beats.Count;

        for (int i = 0; i < beatCount && !session.InDepth; i++)
        {
            session.BeatIndex = i;
            await RunDayBeatAsync(session, monster.Beats[i], forcedExtra: false);
        }

        // 감김상/침면: 종료 시 붕괴 ≥80 이면 추가 비트 1회 강제. (§13.2)
        if (!session.InDepth
            && (monster.SpecialRule == MonsterSpecialRule.Overstay
                || monster.SpecialRule == MonsterSpecialRule.OverstayVeil)
            && maid.Gauge.Get(monster.DemandAxis) >= Tuning.ManagedReleaseMinimumCollapse)
        {
            session.BeatIndex = beatCount;
            await RunDayBeatAsync(session, monster.Beats[beatCount - 1], forcedExtra: true);
        }

        // 과몰입 기벽: 80~99 종료 시 확률 부하 +5, 반응 +1. (§10.2)
        if (!session.InDepth)
        {
            (int chance, int extraLoad) = QuirkEffectResolver.OverImmersion(Content, maid);
            int endValue = maid.Gauge.Get(monster.DemandAxis);

            if (chance > 0
                && endValue >= Tuning.ManagedReleaseMinimumCollapse
                && endValue < Tuning.ControlLossThreshold
                && _campaign.CommitPercent("overimmersion", chance))
            {
                maid.Gauge.Add(monster.DemandAxis, extraLoad);
                session.DayReactionScore += 1;
                CheckDepthEntry(session);
            }
        }

        if (session.InDepth)
            await RunDepthAsync(session, playerControlled: true);

        return await SettleAsync(session);
    }

    // ------------------------------------------------------------
    // 낮 비트
    // ------------------------------------------------------------
    private async YarnTask RunDayBeatAsync(ServiceSessionStateV3 session, ServiceBeatV3 beat, bool forcedExtra)
    {
        MaidStateV3 maid = session.Maid;
        MonsterProfileV3 monster = session.Monster;

        if (!forcedExtra)
            await _nodes.PlayNodeAsync(beat.SituationNodeKey);

        ServiceOptionV3 option;
        var usedAbilities = new List<string>();

        if (forcedExtra)
        {
            // 이탈 판정 실패: 자동 중 옵션, 승인 없음. (§13.2)
            option = FindByIntensity(beat.Options, OptionIntensity.Medium) ?? beat.Options[0];
        }
        else
        {
            ApprovalRequestV3 request = BuildApprovalRequest(session, beat);
            ApprovalResponseV3 response = await _screens.RequestApprovalAsync(request);
            int index = Math.Clamp(response.OptionIndex, 0, beat.Options.Count - 1);
            option = beat.Options[index];

            if (response.UsedAbilityIds != null)
                foreach (string id in response.UsedAbilityIds)
                    if (TryUseAbility(id, dayContext: true)) usedAbilities.Add(id);
        }

        // ---- 부하 판정 (§2.3) ----
        int monsterMod = monster.LoadModifier;
        if (monster.LoadModifierHeavyOnly && option.Intensity != OptionIntensity.Heavy) monsterMod = 0;
        if (monster.SpecialRule == MonsterSpecialRule.TighteningGrip && session.BeatIndex >= 2) monsterMod += 4;
        if (forcedExtra) monsterMod += 6;

        for (int a = 0; a < maid.Aftereffects.Count; a++)
            monsterMod += maid.Aftereffects[a].Definition.DayLoadPenalty;      // 떨림 +2 (§9 개정)

        int aptitude = maid.Aptitude[option.LoadAxis];
        int masteryLv = maid.GetMastery(option.LoadAxis).Level;

        var dice = new CommittingDice(_campaign, "load");
        LoadJudgmentResult load = LoadRangeJudgmentRule.Judge(
            dice, option.Range, monsterMod, aptitude, masteryLv, Tuning);

        // 임상적 거리: 지정 축 2회 굴려 낮은 값. (§10.1)
        if (QuirkEffectResolver.LoadRollTakeLowest(Content, maid, option.LoadAxis))
        {
            LoadJudgmentResult second = LoadRangeJudgmentRule.Judge(
                dice, option.Range, monsterMod, aptitude, masteryLv, Tuning);
            if (second.AppliedLoad < load.AppliedLoad) load = second;
        }

        int applied = load.AppliedLoad;
        int extraReaction = 0;

        // ---- 낮 능력 적용 (§11) ----
        foreach (string id in usedAbilities)
        {
            PlayerAbilityDefinition def = Content.GetAbility(id);
            if (def == null) continue;
            switch (def.EffectKind)
            {
                case AbilityEffectKind.LoadCap:
                    applied = Math.Min(applied, def.Magnitude); break;
                case AbilityEffectKind.LoadRedirectPercent:
                {
                    int moved = applied * def.Magnitude / 100;
                    applied -= moved;
                    maid.Gauge.Add(LowestOtherAxis(maid, option.LoadAxis), moved);
                    break;
                }
                case AbilityEffectKind.ReactionUpgradePlusLoad:
                    extraReaction = 1; applied += def.Magnitude; break;
                case AbilityEffectKind.ConvertLoadToReaction:
                    applied /= 2; session.DayReactionScore += def.Magnitude; break;
            }
        }

        maid.Gauge.Add(option.LoadAxis, applied);
        session.AccumulatedRawLoad = session.AccumulatedRawLoad.AddAxis(option.LoadAxis, load.RawLoad);

        // 잔향: 적용 부하의 20% 를 추가 감응 부하로 반향. (§13.2)
        if (monster.SpecialRule == MonsterSpecialRule.Reverb)
        {
            int echo = applied * 20 / 100;
            if (echo > 0)
            {
                maid.Gauge.Add(BurdenAxis.Empathic, echo);
                session.AccumulatedRawLoad = session.AccumulatedRawLoad.AddAxis(BurdenAxis.Empathic, echo);
            }
        }

        // ---- 반응 산정 (§2.4 + 특이 규칙 + 기벽) ----
        int score = ResolveReactionScore(session, option) + extraReaction;
        session.DayReactionScore += Math.Max(0, score);
        session.LastApprovedIntensity = option.Intensity;

        await _nodes.PlayNodeAsync(option.NodeKey);

        CheckDepthEntry(session);
    }

    private int ResolveReactionScore(ServiceSessionStateV3 session, ServiceOptionV3 option)
    {
        MonsterProfileV3 monster = session.Monster;
        int step = StepOf(option.Intensity);

        if (monster.SpecialRule == MonsterSpecialRule.HeavyReactionEcho
            && option.Intensity == OptionIntensity.Medium)
            step = 2;

        if (QuirkEffectResolver.MediumUpgrades(Content, session.Maid, option))
            step = 2;

        if (monster.SpecialRule == MonsterSpecialRule.DangerCraving)
        {
            int v = session.Maid.Gauge.Get(monster.DemandAxis);
            if (v >= Tuning.ManagedReleaseMinimumCollapse && v < Tuning.ControlLossThreshold)
                step = Math.Min(2, step + 1);
        }

        if (monster.SpecialRule == MonsterSpecialRule.RepetitionBoredom
            && session.LastApprovedIntensity.HasValue
            && session.LastApprovedIntensity.Value == option.Intensity)
            step = Math.Max(0, step - 1);

        return StepToScore(step);
    }

    private static int StepOf(OptionIntensity intensity) => (int)intensity; // 0/1/2
    private static int StepToScore(int step) => step switch { 2 => 3, 1 => 1, _ => 0 };

    private ApprovalRequestV3 BuildApprovalRequest(ServiceSessionStateV3 session, ServiceBeatV3 beat)
    {
        MaidStateV3 maid = session.Maid;
        MonsterProfileV3 monster = session.Monster;
        UnderstandingTier tier = _campaign.Understanding.GetTier(monster.MonsterId, Tuning);

        // 이면잉크: 고도 미만이면 요구축을 감응으로 위장 표시. (§13.2)
        bool masquerade = monster.SpecialRule == MonsterSpecialRule.AxisMasquerade
                          && tier < UnderstandingTier.Advanced;

        var displays = new List<OptionDisplayV3>(beat.Options.Count);
        for (int i = 0; i < beat.Options.Count; i++)
        {
            ServiceOptionV3 option = beat.Options[i];
            BurdenAxis displayAxis = masquerade && option.LoadAxis == monster.DemandAxis
                ? BurdenAxis.Empathic
                : option.LoadAxis;

            bool showsRange = tier >= UnderstandingTier.Advanced;              // §2.5
            int min = option.Range.Min, max = option.Range.Max;
            if (tier >= UnderstandingTier.Complete)
            {
                int mod = monster.LoadModifierHeavyOnly && option.Intensity != OptionIntensity.Heavy
                    ? 0 : monster.LoadModifier;
                min += mod; max += mod;                                        // 완전: 개체 보정 포함
            }

            bool upgraded =
                (monster.SpecialRule == MonsterSpecialRule.HeavyReactionEcho
                 && option.Intensity == OptionIntensity.Medium)
                || QuirkEffectResolver.MediumUpgrades(Content, maid, option);

            displays.Add(new OptionDisplayV3(i, option.Intensity, displayAxis, showsRange, min, max, upgraded));
        }

        return new ApprovalRequestV3(session, session.BeatIndex, displays, CollectUsableIds(dayContext: true));
    }

    // ------------------------------------------------------------
    // 붕괴심층 (§4)
    // ------------------------------------------------------------
    private void CheckDepthEntry(ServiceSessionStateV3 session)
    {
        if (session.InDepth) return;
        if (!session.Maid.Gauge.TryFindAxisAtOrAbove(Tuning.ControlLossThreshold, out BurdenAxis axis)) return;
        session.InDepth = true;
        session.DepthAxis = axis;
    }

    /// <summary>심층 루프. playerControlled=false 는 심야 사건(§6.2): 개입·회수 선택 없음, 비트 상한 존재.</summary>
    public async YarnTask RunDepthAsync(ServiceSessionStateV3 session, bool playerControlled, int maxBeats = int.MaxValue)
    {
        MaidStateV3 maid = session.Maid;
        MonsterProfileV3 monster = session.Monster;

        if (playerControlled && session.Protocol != null)
            await _nodes.PlayNodeAsync(session.Protocol.ControlLossNodeName);

        // 첫 진입 보장: 즉시 탈출해도 목격 기록·이해도·회상 플래그. (§3.3, §4.4)
        UnderstandingRule.GrantDepthWitness(_campaign, monster, maid);
        _campaign.CountWitness(monster.Species.ToResearchType());
        _campaign.Understanding.TryClaimOneTime("depthpage", monster.MonsterId, maid.MaidId);

        UnderstandingTier tier = _campaign.Understanding.GetTier(monster.MonsterId, Tuning);
        bool negateMonsterMods = false;

        while (session.DepthBeatCount < maxBeats)
        {
            session.DepthBeatCount++;

            int recoveryShiftFromAbility = ResolvePassiveRecoveryShift(maid);
            DepthBandLayout layout = QuirkEffectResolver.BuildDepthLayout(
                Content, maid, negateMonsterMods ? 0 : recoveryShiftFromAbility, Tuning);
            if (negateMonsterMods) layout = Tuning.DepthStandardLayout;

            int interventionDelta = 0;
            int? cap = null;
            bool forceRecovery = false;

            if (playerControlled)
            {
                bool revealed = tier >= UnderstandingTier.Advanced
                    || IsPassiveActive(AbilityEffectKind.RevealDepthTable, monster)
                    || _campaign.ShopLevel >= 5;                                // §8 Lv5 사전 분석
                DepthBand? predicted = HasUsable(AbilityEffectKind.PredictBand)
                    ? PredictBand(layout, maid, monster, tier) : (DepthBand?)null;

                IReadOnlyList<string> chosen = await _screens.RequestDepthInterventionAsync(
                    new DepthInterventionRequestV3(session, session.DepthBeatCount, layout,
                        revealed, predicted, CollectUsableIds(dayContext: false)));

                if (chosen != null)
                {
                    foreach (string id in chosen)
                    {
                        PlayerAbilityDefinition def = Content.GetAbility(id);
                        if (def == null || !TryUseAbility(id, dayContext: false)) continue;
                        switch (def.EffectKind)
                        {
                            case AbilityEffectKind.DepthDelta:
                            case AbilityEffectKind.MaidPredictMinus:
                                interventionDelta += def.Magnitude; break;
                            case AbilityEffectKind.DepthMaxCap:
                                cap = def.Magnitude; break;
                            case AbilityEffectKind.ForceRecoveryWindow:
                                forceRecovery = true; break;
                            case AbilityEffectKind.SealSpecialResult:
                                session.SpecialSealed = true; break;
                            case AbilityEffectKind.NegateMonsterMods:
                                negateMonsterMods = true; break;
                        }
                    }
                }
            }

            DepthRollResult roll;

            if (forceRecovery)
            {
                roll = new DepthRollResult(0, 0, 0, false, 1, DepthBand.Recovery, 0);
            }
            else
            {
                roll = RollDepth(session, layout, interventionDelta, cap, tier);

                if (playerControlled)
                    roll = await OfferPostRollAsync(session, roll, layout);
            }

            // 침면: 첫 심층 비트의 회수 무효. (§13.2)
            if (roll.Band == DepthBand.Recovery
                && session.DepthBeatCount == 1
                && monster.SpecialRule == MonsterSpecialRule.OverstayVeil
                && !forceRecovery)
            {
                session.FirstRecoverySuppressed = true;
                roll = DepthDiceRule.Interpret(
                    Math.Max(roll.FinalValue, layout.RecoveryMax + 1),
                    new DepthRollInput(0), layout, Tuning);
            }

            // 봉인·거절: 특수/치명 하향. (§11)
            DepthBand band = roll.Band;
            if (band == DepthBand.Special && session.SpecialSealed) band = DepthBand.Fatal;
            if (band == DepthBand.Fatal && session.RemovedActionNode == monster.DepthActions.FatalNodeKey)
                band = DepthBand.Risky;

            if (band == DepthBand.Recovery)
            {
                await _nodes.PlayNodeAsync($"Depth_Recover_{maid.MaidId}");

                bool escape = true;
                if (playerControlled)
                    escape = await _screens.RequestRecoveryChoiceAsync(session);

                if (escape)
                {
                    session.EndKind = SettlementOutcomeKind.DepthEscape;
                    ApplyEscapeAftereffects(session);
                    return;
                }

                continue;   // 한 번 더 남긴다. (§3.3)
            }

            // 행동 노드 + 붕괴 가산
            string node = band switch
            {
                DepthBand.Special => monster.DepthActions.SpecialNodeKey,
                DepthBand.Fatal => monster.DepthActions.FatalNodeKey,
                _ => monster.DepthActions.RiskyNodeKey,
            };
            await _nodes.PlayNodeAsync(node);

            maid.Gauge.Add(session.DepthAxis, roll.CollapseGain);

            if (band == DepthBand.Special)
            {
                maid.AddAftereffect(Content.GetAftereffect("se_brand"));
                _campaign.Understanding.TryClaimOneTime("special", monster.MonsterId, maid.MaidId);
            }
            _campaign.Understanding.TryClaimOneTime($"recall_{band}", monster.MonsterId, maid.MaidId);

            // ---- 200 판정 (§5) ----
            if (maid.Gauge.Get(session.DepthAxis) >= Tuning.TotalCollapseThreshold)
            {
                if (TryStopAt199(session)) continue;

                TotalCollapseOutcome outcome = TotalCollapseRule.Resolve(maid, session.Protocol, Tuning);
                session.EndKind = SettlementOutcomeKind.TotalCollapse;

                if (outcome.NodeToPlay != null)
                    await _nodes.PlayNodeAsync(outcome.NodeToPlay);

                if (outcome.Rescued)
                {
                    maid.AddAftereffect(Content.GetAftereffect(outcome.AftereffectId));
                    maid.AddQuirk(outcome.AccidentQuirkId, isAccident: true);
                }
                else
                {
                    _campaign.RuinedRouteMaidIds.Add(maid.MaidId);
                }

                return;
            }
        }
    }

    private DepthRollResult RollDepth(
        ServiceSessionStateV3 session, in DepthBandLayout layout,
        int interventionDelta, int? cap, UnderstandingTier tier)
    {
        MaidStateV3 maid = session.Maid;

        int status = QuirkEffectResolver.StatusDieModifier(
            Content, maid, session.Monster.Species, Tuning);
        int understanding = tier >= UnderstandingTier.Complete
            ? Tuning.DepthFullUnderstandingDieModifier : 0;                    // §8.2

        var input = new DepthRollInput(
            demandAxisAptitude: maid.Aptitude[session.DepthAxis],
            statusEffectModifier: status,
            understandingModifier: understanding,
            interventionModifier: interventionDelta,
            maxValueCap: cap);

        int baseRoll = _campaign.CommitRoll("depth", 1, 99);
        return DepthDiceRule.Interpret(baseRoll, input, layout, Tuning);
    }

    private async YarnTask<DepthRollResult> OfferPostRollAsync(
        ServiceSessionStateV3 session, DepthRollResult roll, DepthBandLayout layout)
    {
        while (true)
        {
            var post = new List<string>(2);
            AddIfUsable(post, AbilityEffectKind.DepthReroll);
            AddIfUsable(post, AbilityEffectKind.DepthBandDowngrade);
            if (session.Maid.MaidId == "maid_shion") AddIfUsable(post, AbilityEffectKind.MaidDepthReroll);

            if (post.Count == 0) return roll;

            DepthRollDecisionV3 decision = await _screens.PresentDepthRollAsync(session, roll, post);

            if (decision.RerollAbilityId != null && TryUseAbility(decision.RerollAbilityId, dayContext: false))
            {
                UnderstandingTier tier = _campaign.Understanding.GetTier(session.Monster.MonsterId, Tuning);
                roll = RollDepth(session, layout, 0, null, tier);
                continue;                                                       // 재굴림 결과에 다시 제안
            }

            if (decision.DowngradeAbilityId != null && TryUseAbility(decision.DowngradeAbilityId, dayContext: false))
            {
                DepthBand down = roll.Band == DepthBand.Recovery
                    ? DepthBand.Recovery : (DepthBand)((int)roll.Band - 1);
                int gain = DepthDiceRule.CalculateCollapseGain(roll.FinalValue, down, Tuning);
                return new DepthRollResult(roll.BaseRoll, roll.RawModifierSum,
                    roll.ClampedModifierSum, roll.WasCapped, roll.FinalValue, down, gain);
            }

            return roll;
        }
    }

    private void ApplyEscapeAftereffects(ServiceSessionStateV3 session)
    {
        // 탈출 = 통제선 아래로 데리고 나온다: 게이지 99 회수. (§3.4 개정 — 시뮬 검증)
        // 100+ 로 남기면 관리 붕괴(80~99) 대상이 못 되고 다음 접객 1비트째 재심층 → 수입 0 나선.
        // 비용은 ×0.5 결산 + 떨림 + 관계 노선 플래그로 이미 지불된다. 99 는 여전히 벼랑 끝이다.
        MaidStateV3 maid = session.Maid;
        int cap = Tuning.ControlLossThreshold - 1;
        if (maid.Gauge.Get(session.DepthAxis) > cap)
            maid.Gauge.SetValue(session.DepthAxis, cap);

        maid.AddAftereffect(Content.GetAftereffect("se_tremor"));
        maid.MarkDepthScar();
    }

    private bool TryStopAt199(ServiceSessionStateV3 session)
    {
        if (session.StopAt199Consumed) return false;

        foreach (PlayerAbilityDefinition def in EachUsable())
        {
            if (def.EffectKind != AbilityEffectKind.StopAt199) continue;
            if (def.OwnerMaidId != null && def.OwnerMaidId != session.Maid.MaidId) continue;

            _campaign.Abilities.MarkUsed(def);
            session.StopAt199Consumed = true;
            session.Maid.Gauge.SetValue(session.DepthAxis, Tuning.TotalCollapseThreshold - 1);
            return true;                                                        // 199 정지. (§11.3)
        }

        return false;
    }

    // ------------------------------------------------------------
    // 결산 (§7.2)
    // ------------------------------------------------------------
    private async YarnTask<SettlementV3Result> SettleAsync(ServiceSessionStateV3 session)
    {
        MaidStateV3 maid = session.Maid;
        BurdenAxis settleAxis = session.InDepth ? session.DepthAxis : session.Monster.DemandAxis;

        SettlementV3Result result = SettlementRuleV3.Calculate(
            session.EndKind,
            session.DayReactionScore,
            session.Satisfaction,
            session.Monster.RequiredSatisfaction,
            maid.Gauge.Get(settleAxis),
            Tuning);

        _campaign.Ledger.Earn(result.Energy);

        // 숙련 XP: 완화 전 원본 부하. (§12.3)
        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            BurdenAxis axis = BurdenAxes.FromIndex(i);
            maid.GetMastery(axis).AddExperience(session.AccumulatedRawLoad[axis]);
        }

        UnderstandingRule.GrantServiceComplete(_campaign, session.Monster.MonsterId, maid);
        _campaign.CountService(session.Monster.Species.ToResearchType());

        await _screens.PresentSettlementAsync(session, result);
        _campaign.Phase = CampaignPhaseV3.SlotBoundary;
        return result;
    }

    // ------------------------------------------------------------
    // 능력 헬퍼
    // ------------------------------------------------------------
    private IEnumerable<PlayerAbilityDefinition> EachUsable()
    {
        IReadOnlyList<PlayerAbilityDefinition> all = Content.Abilities;
        for (int i = 0; i < all.Count; i++)
            if (_campaign.Abilities.CanUse(all[i])) yield return all[i];
    }

    private static readonly AbilityEffectKind[] DayKinds =
    {
        AbilityEffectKind.LoadCap, AbilityEffectKind.LoadRedirectPercent,
        AbilityEffectKind.ReactionUpgradePlusLoad, AbilityEffectKind.ConvertLoadToReaction,
    };

    private static readonly AbilityEffectKind[] DepthPreKinds =
    {
        AbilityEffectKind.DepthDelta, AbilityEffectKind.DepthMaxCap,
        AbilityEffectKind.ForceRecoveryWindow, AbilityEffectKind.SealSpecialResult,
        AbilityEffectKind.NegateMonsterMods, AbilityEffectKind.MaidPredictMinus,
    };

    private List<string> CollectUsableIds(bool dayContext)
    {
        AbilityEffectKind[] kinds = dayContext ? DayKinds : DepthPreKinds;
        var ids = new List<string>();
        foreach (PlayerAbilityDefinition def in EachUsable())
            if (Array.IndexOf(kinds, def.EffectKind) >= 0) ids.Add(def.Id);
        return ids;
    }

    private bool TryUseAbility(string id, bool dayContext)
    {
        PlayerAbilityDefinition def = Content.GetAbility(id);
        if (def == null || !_campaign.Abilities.CanUse(def)) return false;
        _campaign.Abilities.MarkUsed(def);
        return true;
    }

    private void AddIfUsable(List<string> list, AbilityEffectKind kind)
    {
        foreach (PlayerAbilityDefinition def in EachUsable())
            if (def.EffectKind == kind && !list.Contains(def.Id)) list.Add(def.Id);
    }

    private bool HasUsable(AbilityEffectKind kind)
    {
        foreach (PlayerAbilityDefinition def in EachUsable())
            if (def.EffectKind == kind) return true;
        return false;
    }

    private bool IsPassiveActive(AbilityEffectKind kind, MonsterProfileV3 monster)
    {
        foreach (PlayerAbilityDefinition def in EachUsable())
            if (def.EffectKind == kind
                && (def.KnowledgeGate != KnowledgeGateKind.TypeWitnessCount
                    || def.KnowledgeType == monster.Species.ToResearchType()))
                return true;
        return false;
    }

    private int ResolvePassiveRecoveryShift(MaidStateV3 maid)
    {
        int shift = 0;
        foreach (PlayerAbilityDefinition def in EachUsable())
            if (def.EffectKind == AbilityEffectKind.MaidRecoveryShift
                && def.OwnerMaidId == maid.MaidId)
                shift += def.Magnitude;
        return shift;
    }

    private DepthBand PredictBand(
        in DepthBandLayout layout, MaidStateV3 maid, MonsterProfileV3 monster, UnderstandingTier tier)
    {
        // 최빈 구간 = 보정 후 기대 최종값(50 + 보정)이 속하는 구간. (§11.2 징후 판독)
        int status = QuirkEffectResolver.StatusDieModifier(Content, maid, monster.Species, Tuning);
        int understanding = tier >= UnderstandingTier.Complete
            ? Tuning.DepthFullUnderstandingDieModifier : 0;
        int expected = Math.Clamp(
            50 - maid.Aptitude[monster.DemandAxis] * Tuning.DepthAptitudeDiePerPoint + status + understanding,
            1, 99);
        return layout.Resolve(expected);
    }

    private static ServiceOptionV3 FindByIntensity(IReadOnlyList<ServiceOptionV3> options, OptionIntensity intensity)
    {
        for (int i = 0; i < options.Count; i++)
            if (options[i].Intensity == intensity) return options[i];
        return null;
    }

    private static BurdenAxis LowestOtherAxis(MaidStateV3 maid, BurdenAxis exclude)
    {
        BurdenAxis best = BurdenAxis.Physical;
        int bestValue = int.MaxValue;
        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            BurdenAxis axis = BurdenAxes.FromIndex(i);
            if (axis == exclude) continue;
            int v = maid.Gauge.Get(axis);
            if (v < bestValue) { bestValue = v; best = axis; }
        }
        return best;
    }
}
