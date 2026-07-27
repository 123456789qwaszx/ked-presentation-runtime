using NUnit.Framework;

/// <summary>
/// v3 §18 1단계 순수 계산 레이어 검증.
/// 수치 기대값은 전부 guesthouse_design_v3.md 의 표에서 온다 — 절 번호를 각 테스트에 병기.
/// </summary>
public sealed class GuesthouseV3RulesTests
{
    private static GuesthouseTuningV3 Standard => GuesthouseTuningV3.CreateStandard();

    // ------------------------------------------------------------
    // DeterministicRng
    // ------------------------------------------------------------

    [Test]
    public void Rng_SameSeed_ProducesSameSequence()
    {
        var a = new DeterministicRng(12345UL);
        var b = new DeterministicRng(12345UL);

        for (int i = 0; i < 100; i++)
            Assert.AreEqual(a.RollDie99(), b.RollDie99());
    }

    [Test]
    public void Rng_RestoreState_ContinuesIdentically()
    {
        var source = new DeterministicRng(777UL);
        source.RollDie99();
        source.RollDie99();

        ulong committed = source.State;
        int expected = source.RollDie99();

        var restored = new DeterministicRng(0UL);
        restored.RestoreState(committed);

        Assert.AreEqual(expected, restored.RollDie99(),
            "커밋된 State 복원 후 다음 굴림이 동일해야 한다 (§14 판정 커밋)");
    }

    [Test]
    public void Rng_RangeIsInclusiveAndBounded()
    {
        var rng = new DeterministicRng(42UL);

        for (int i = 0; i < 1000; i++)
        {
            int value = rng.NextInclusive(1, 99);
            Assert.That(value, Is.InRange(1, 99));
        }
    }

    // ------------------------------------------------------------
    // DepthBandLayout (§4.3)
    // ------------------------------------------------------------

    [Test]
    public void Layout_Standard_ResolvesDocBoundaries()
    {
        DepthBandLayout layout = DepthBandLayout.Standard;

        Assert.AreEqual(DepthBand.Recovery, layout.Resolve(1));
        Assert.AreEqual(DepthBand.Recovery, layout.Resolve(20));
        Assert.AreEqual(DepthBand.Risky, layout.Resolve(21));
        Assert.AreEqual(DepthBand.Risky, layout.Resolve(60));
        Assert.AreEqual(DepthBand.Fatal, layout.Resolve(61));
        Assert.AreEqual(DepthBand.Fatal, layout.Resolve(94));
        Assert.AreEqual(DepthBand.Special, layout.Resolve(95));
        Assert.AreEqual(DepthBand.Special, layout.Resolve(99));
    }

    [Test]
    public void Layout_ShionTrait_ExtendsRecoveryTo28()
    {
        // §12.1 시온: 회수 1~20 → 1~28
        DepthBandLayout layout = DepthBandLayout.Standard.ShiftRecoveryMax(+8, Standard.DepthMinBandWidth);

        Assert.AreEqual(28, layout.RecoveryMax);
        Assert.AreEqual(DepthBand.Recovery, layout.Resolve(28));
        Assert.AreEqual(DepthBand.Risky, layout.Resolve(29));
    }

    [Test]
    public void Layout_RuiTrait_LowersFatalStartTo58()
    {
        // §12.1 루이: 치명 61~94 → 58~94 (= 위험 상한 60 → 57)
        DepthBandLayout layout = DepthBandLayout.Standard.ShiftRiskyMax(-3, Standard.DepthMinBandWidth);

        Assert.AreEqual(57, layout.RiskyMax);
        Assert.AreEqual(DepthBand.Fatal, layout.Resolve(58));
    }

    [Test]
    public void Layout_OverShift_IsClampedByMinBandWidth()
    {
        int width = Standard.DepthMinBandWidth; // 4

        // 회수 상한을 극단으로 밀어도 뒤 3구간이 각각 최소 폭을 유지해야 한다.
        DepthBandLayout layout = DepthBandLayout.Standard.ShiftRecoveryMax(+500, width);

        Assert.AreEqual(99 - width * 3, layout.RecoveryMax);
        Assert.That(layout.RiskyMax - layout.RecoveryMax, Is.GreaterThanOrEqualTo(width));
        Assert.That(layout.FatalMax - layout.RiskyMax, Is.GreaterThanOrEqualTo(width));
        Assert.That(99 - layout.FatalMax, Is.GreaterThanOrEqualTo(width));

        // 반대 방향도 구간이 소멸하지 않는다.
        DepthBandLayout shrunk = DepthBandLayout.Standard.ShiftRecoveryMax(-500, width);
        Assert.AreEqual(width, shrunk.RecoveryMax);
    }

    // ------------------------------------------------------------
    // DepthDiceRule (§4.2~4.3)
    // ------------------------------------------------------------

    [Test]
    public void Depth_AptitudeAppliesMinusThreePerPoint()
    {
        // 기본 78, 적성 4 → -12, 최종 66 = 치명 구간, 가산 ⌈66/2⌉ = 33
        var input = new DepthRollInput(demandAxisAptitude: 4);

        DepthRollResult result = DepthDiceRule.Interpret(78, input, DepthBandLayout.Standard, Standard);

        Assert.AreEqual(-12, result.ClampedModifierSum);
        Assert.AreEqual(66, result.FinalValue);
        Assert.AreEqual(DepthBand.Fatal, result.Band);
        Assert.AreEqual(33, result.CollapseGain);
    }

    [Test]
    public void Depth_ModifierSum_IsClampedAtThirty()
    {
        // 적성 4(-12) + 개입 -30 = 원합 -42 → 클램프 -30
        var input = new DepthRollInput(
            demandAxisAptitude: 4,
            interventionModifier: -30);

        DepthRollResult result = DepthDiceRule.Interpret(50, input, DepthBandLayout.Standard, Standard);

        Assert.AreEqual(-42, result.RawModifierSum);
        Assert.AreEqual(-30, result.ClampedModifierSum);
        Assert.IsTrue(result.WasModifierClamped);
        Assert.AreEqual(20, result.FinalValue);
        Assert.AreEqual(DepthBand.Recovery, result.Band);
        Assert.AreEqual(0, result.CollapseGain);
    }

    [Test]
    public void Depth_MaxValueCap_AppliesAfterClamp()
    {
        // 기본 95 + 상태 +30(각인류 중첩 가정) → 클램프 후에도 99, 상한 50 이 별도 규칙으로 잘라낸다. (§4.2)
        var input = new DepthRollInput(
            demandAxisAptitude: 0,
            statusEffectModifier: 30,
            maxValueCap: 50);

        DepthRollResult result = DepthDiceRule.Interpret(95, input, DepthBandLayout.Standard, Standard);

        Assert.IsTrue(result.WasCapped);
        Assert.AreEqual(50, result.FinalValue);
        Assert.AreEqual(DepthBand.Risky, result.Band);
        Assert.AreEqual(25, result.CollapseGain);
    }

    [Test]
    public void Depth_RecoveryBand_GainsZero_AndOpensWindow()
    {
        var input = new DepthRollInput(demandAxisAptitude: 0);

        DepthRollResult result = DepthDiceRule.Interpret(15, input, DepthBandLayout.Standard, Standard);

        Assert.AreEqual(DepthBand.Recovery, result.Band);
        Assert.AreEqual(0, result.CollapseGain);
        Assert.IsTrue(result.OpensRecoveryWindow);
        Assert.IsFalse(result.InflictsBrand);
    }

    [Test]
    public void Depth_SpecialBand_SignalsBrand()
    {
        var input = new DepthRollInput(demandAxisAptitude: 0);

        DepthRollResult result = DepthDiceRule.Interpret(97, input, DepthBandLayout.Standard, Standard);

        Assert.AreEqual(DepthBand.Special, result.Band);
        Assert.IsTrue(result.InflictsBrand);
        Assert.AreEqual(49, result.CollapseGain); // ⌈97/2⌉
    }

    [Test]
    public void Depth_CollapseGain_IsCeilingOfHalf()
    {
        var input = new DepthRollInput(demandAxisAptitude: 0);

        Assert.AreEqual(36, DepthDiceRule.Interpret(71, input, DepthBandLayout.Standard, Standard).CollapseGain);
        Assert.AreEqual(36, DepthDiceRule.Interpret(72, input, DepthBandLayout.Standard, Standard).CollapseGain);
    }

    [Test]
    public void Depth_FinalValue_NeverLeavesOneToNinetyNine()
    {
        var heavyMinus = new DepthRollInput(demandAxisAptitude: 4, interventionModifier: -18); // -30 클램프
        Assert.AreEqual(1, DepthDiceRule.Interpret(3, heavyMinus, DepthBandLayout.Standard, Standard).FinalValue);

        var heavyPlus = new DepthRollInput(demandAxisAptitude: 0, statusEffectModifier: 30);
        Assert.AreEqual(99, DepthDiceRule.Interpret(98, heavyPlus, DepthBandLayout.Standard, Standard).FinalValue);
    }

    [Test]
    public void Depth_AverageStay_MatchesDocTarget()
    {
        // §4.3 검증: 무보정 심층 진입(100) 시 평균 2~5비트 안에 200 또는 회수.
        // §17 회귀 지표의 축소판 — 시드 고정 통계 새니티.
        var rng = new DeterministicRng(20260727UL);
        var tuning = Standard;
        var input = new DepthRollInput(demandAxisAptitude: 0);

        int totalBeats = 0;
        const int trials = 500;

        for (int t = 0; t < trials; t++)
        {
            int collapse = tuning.ControlLossThreshold;
            int beats = 0;

            while (collapse < tuning.TotalCollapseThreshold && beats < 30)
            {
                DepthRollResult roll = DepthDiceRule.Roll(rng, input, DepthBandLayout.Standard, tuning);
                beats++;

                if (roll.OpensRecoveryWindow)
                    break; // 즉시 탈출 정책

                collapse += roll.CollapseGain;
            }

            totalBeats += beats;
        }

        double average = (double)totalBeats / trials;

        Assert.That(average, Is.InRange(2.0, 5.0),
            $"심층 평균 체류 비트 {average:0.00} — 목표 2~5 (§4.3, §17)");
    }

    // ------------------------------------------------------------
    // LoadRangeJudgmentRule (§2.3)
    // ------------------------------------------------------------

    [Test]
    public void Load_Formula_MatchesDoc()
    {
        // 강(15~23)에서 20 굴림, 개체 보정 +2, 적성 4, 숙련 1
        // 원본 = 22, 적용 = 22 - 8 - 1 = 13
        LoadJudgmentResult result = LoadRangeJudgmentRule.Interpret(
            rangeRoll: 20,
            monsterLoadModifier: 2,
            demandAxisAptitude: 4,
            masteryLevel: 1,
            Standard);

        Assert.AreEqual(22, result.RawLoad);
        Assert.AreEqual(13, result.AppliedLoad);
        Assert.IsFalse(result.WasFloored);
    }

    [Test]
    public void Load_MitigationCannotGoBelowFloor()
    {
        // 약(6~10)에서 6 굴림, 적성 4 → 6 - 8 = -2 → 바닥 4
        LoadJudgmentResult result = LoadRangeJudgmentRule.Interpret(
            rangeRoll: 6,
            monsterLoadModifier: 0,
            demandAxisAptitude: 4,
            masteryLevel: 0,
            Standard);

        Assert.AreEqual(Standard.DayMinimumAppliedLoad, result.AppliedLoad);
        Assert.IsTrue(result.WasFloored);
        Assert.AreEqual(6, result.RawLoad, "완화 전 원본은 보존된다 — 숙련 경험치 기준 (§12.3)");
    }

    [Test]
    public void Load_RollStaysInsideRange()
    {
        var rng = new DeterministicRng(9UL);
        LoadRange heavy = LoadRange.Heavy;

        for (int i = 0; i < 500; i++)
        {
            LoadJudgmentResult result = LoadRangeJudgmentRule.Judge(
                rng, heavy, monsterLoadModifier: 0, demandAxisAptitude: 0, masteryLevel: 0, Standard);

            Assert.That(result.RangeRoll, Is.InRange(heavy.Min, heavy.Max));
        }
    }

    // ------------------------------------------------------------
    // NeglectRule (§6.2)
    // ------------------------------------------------------------

    [Test]
    public void Neglect_BelowDangerBand_NaturallyRecovers()
    {
        var dice = new FixedDiceSource(1);

        NeglectJudgment judgment = NeglectRule.Judge(
            dice, highestAxisCollapse: 55, hasAftereffect: false, hasSpecialQuirk: false,
            NeglectRule.NeglectChances.From(Standard), Standard);

        Assert.AreEqual(NeglectCollapseOutcome.NaturalRecovery, judgment.Outcome);
        Assert.AreEqual(45, judgment.CollapseAfter);
    }

    [Test]
    public void Neglect_DangerBand_Thresholds_60_25_15()
    {
        var chances = NeglectRule.NeglectChances.From(Standard);

        // 1~60 유지
        NeglectJudgment hold = NeglectRule.Judge(
            new FixedDiceSource(60), 85, false, false, chances, Standard);
        Assert.AreEqual(NeglectCollapseOutcome.DangerHold, hold.Outcome);
        Assert.AreEqual(85, hold.CollapseAfter);

        // 61~85 자기해소 (두 번째 굴림 = 기벽 판정 50%)
        NeglectJudgment self = NeglectRule.Judge(
            new FixedDiceSource(61, 50), 85, false, false, chances, Standard);
        Assert.AreEqual(NeglectCollapseOutcome.SelfRelease, self.Outcome);
        Assert.AreEqual(Standard.NeglectSelfReleaseTargetCollapse, self.CollapseAfter);
        Assert.IsTrue(self.GainsAccidentQuirk);

        NeglectJudgment selfNoQuirk = NeglectRule.Judge(
            new FixedDiceSource(85, 51), 85, false, false, chances, Standard);
        Assert.AreEqual(NeglectCollapseOutcome.SelfRelease, selfNoQuirk.Outcome);
        Assert.IsFalse(selfNoQuirk.GainsAccidentQuirk);

        // 86~100 심야 사건
        NeglectJudgment incident = NeglectRule.Judge(
            new FixedDiceSource(86), 85, false, false, chances, Standard);
        Assert.AreEqual(NeglectCollapseOutcome.NightIncident, incident.Outcome);
        Assert.AreEqual(Standard.NightIncidentDepthBeats, incident.IncidentDepthBeats);
        Assert.AreEqual(-1, incident.CollapseAfter, "심야 사건의 붕괴는 심층 결과가 정한다");
    }

    [Test]
    public void Neglect_AftereffectAndQuirk_FlagsIndependent()
    {
        // 0~79 구간이라 붕괴 굴림 없음 → 첫 굴림이 곧 기벽 요구 판정(25%).
        NeglectJudgment scheduled = NeglectRule.Judge(
            new FixedDiceSource(25), 30, hasAftereffect: true, hasSpecialQuirk: true,
            NeglectRule.NeglectChances.From(Standard), Standard);

        Assert.IsTrue(scheduled.AdvancesAftereffectRecovery);
        Assert.IsTrue(scheduled.SchedulesQuirkRequest);

        NeglectJudgment notScheduled = NeglectRule.Judge(
            new FixedDiceSource(26), 30, hasAftereffect: false, hasSpecialQuirk: true,
            NeglectRule.NeglectChances.From(Standard), Standard);

        Assert.IsFalse(notScheduled.AdvancesAftereffectRecovery);
        Assert.IsFalse(notScheduled.SchedulesQuirkRequest);
    }

    [Test]
    public void Neglect_NightOwlQuirk_ChangesChances()
    {
        // qk_acc_nightowl: 유지 60→50, 자기해소 25→35 (§10.2)
        var owl = new NeglectRule.NeglectChances(holdPercent: 50, selfReleasePercent: 35);

        NeglectJudgment self = NeglectRule.Judge(
            new FixedDiceSource(51, 100), 90, false, false, owl, Standard);
        Assert.AreEqual(NeglectCollapseOutcome.SelfRelease, self.Outcome);

        NeglectJudgment incident = NeglectRule.Judge(
            new FixedDiceSource(86), 90, false, false, owl, Standard);
        Assert.AreEqual(NeglectCollapseOutcome.NightIncident, incident.Outcome);
    }

    // ------------------------------------------------------------
    // SettlementRuleV3 (§7.2)
    // ------------------------------------------------------------

    [Test]
    public void Settlement_DocWorkedExample_Collapse86_Yields210()
    {
        // §7.2 예시: 반응 7점, 만족 70/60, 종료 붕괴 86 → 7 × 10 × 3.0 = 210
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.Normal,
            reactionScore: 7, satisfaction: 70, requiredSatisfaction: 60,
            endCollapse: 86, Standard);

        Assert.AreEqual(3.0f, result.AppliedMultiplier);
        Assert.AreEqual(210, result.Energy);
    }

    [Test]
    public void Settlement_DocWorkedExample_Collapse74_Yields105()
    {
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.Normal,
            reactionScore: 7, satisfaction: 70, requiredSatisfaction: 60,
            endCollapse: 74, Standard);

        Assert.AreEqual(1.5f, result.AppliedMultiplier);
        Assert.AreEqual(105, result.Energy);
    }

    [Test]
    public void Settlement_SatisfactionShortfall_DowngradesOneStep()
    {
        // 80~99 착지인데 만족 미달 → ×3.0 이 아니라 ×1.5
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.Normal,
            reactionScore: 5, satisfaction: 50, requiredSatisfaction: 60,
            endCollapse: 86, Standard);

        Assert.AreEqual(3.0f, result.BaseMultiplier);
        Assert.AreEqual(1.5f, result.AppliedMultiplier);
        Assert.IsTrue(result.WasDowngraded);
        Assert.AreEqual(75, result.Energy);
    }

    [Test]
    public void Settlement_ShortfallAtLowestNormalBand_FallsToEscapeMultiplier()
    {
        // ×1.0 에서 미달 → 사다리 최저 단 = 심층 탈출 배율 0.5
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.Normal,
            reactionScore: 4, satisfaction: 40, requiredSatisfaction: 60,
            endCollapse: 30, Standard);

        Assert.AreEqual(0.5f, result.AppliedMultiplier);
        Assert.AreEqual(20, result.Energy);
    }

    [Test]
    public void Settlement_DepthEscape_UsesHalfMultiplier()
    {
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.DepthEscape,
            reactionScore: 6, satisfaction: 60, requiredSatisfaction: 60,
            endCollapse: 130, Standard);

        Assert.AreEqual(0.5f, result.AppliedMultiplier);
        Assert.AreEqual(30, result.Energy);
    }

    [Test]
    public void Settlement_DepthEscapeWithShortfall_CannotGoBelowFloor()
    {
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.DepthEscape,
            reactionScore: 6, satisfaction: 10, requiredSatisfaction: 60,
            endCollapse: 130, Standard);

        Assert.AreEqual(0.5f, result.AppliedMultiplier);
    }

    [Test]
    public void Settlement_TotalCollapse_YieldsZero()
    {
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.TotalCollapse,
            reactionScore: 9, satisfaction: 90, requiredSatisfaction: 60,
            endCollapse: 200, Standard);

        Assert.AreEqual(0, result.Energy);
        Assert.AreEqual(0f, result.AppliedMultiplier);
    }

    [Test]
    public void Settlement_NormalKindWithCollapseOver100_DefensivelyTreatsAsEscape()
    {
        SettlementV3Result result = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.Normal,
            reactionScore: 6, satisfaction: 60, requiredSatisfaction: 60,
            endCollapse: 105, Standard);

        Assert.AreEqual(0.5f, result.AppliedMultiplier);
    }

    [Test]
    public void Settlement_TheCliff_99Vs100()
    {
        // 이 게임의 핵심 낙차: 99 ×3.0 vs 100 ×0.5 (§1.1)
        SettlementV3Result at99 = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.Normal, 7, 70, 60, endCollapse: 99, Standard);
        SettlementV3Result at100 = SettlementRuleV3.Calculate(
            SettlementOutcomeKind.DepthEscape, 7, 70, 60, endCollapse: 100, Standard);

        Assert.AreEqual(210, at99.Energy);
        Assert.AreEqual(35, at100.Energy);
    }

    // ------------------------------------------------------------
    // DesireLedger (§7.1)
    // ------------------------------------------------------------

    [Test]
    public void Ledger_Earn_WritesAllThreeBooks()
    {
        var ledger = new DesireLedger();
        ledger.Earn(30);

        Assert.AreEqual(30, ledger.Today);
        Assert.AreEqual(30, ledger.Held);
        Assert.AreEqual(30, ledger.Lifetime);
    }

    [Test]
    public void Ledger_EarnNight_ExcludesToday()
    {
        var ledger = new DesireLedger();
        ledger.Earn(100);
        ledger.EarnNight(40);

        Assert.AreEqual(100, ledger.Today);
        Assert.AreEqual(140, ledger.Held);
        Assert.AreEqual(140, ledger.Lifetime);
    }

    [Test]
    public void Ledger_Spend_OnlyLowersHeld_AndFailsWhenShort()
    {
        var ledger = new DesireLedger();
        ledger.Earn(200);

        Assert.IsTrue(ledger.TrySpend(150));
        Assert.AreEqual(200, ledger.Today);
        Assert.AreEqual(50, ledger.Held);
        Assert.AreEqual(200, ledger.Lifetime);

        Assert.IsFalse(ledger.TrySpend(51));
        Assert.AreEqual(50, ledger.Held, "실패한 소비는 아무것도 바꾸지 않는다");
    }

    [Test]
    public void Ledger_StartNewDay_ResetsOnlyToday()
    {
        var ledger = new DesireLedger();
        ledger.Earn(120);
        Assert.IsTrue(ledger.MeetsQuota(120));

        ledger.StartNewDay();

        Assert.AreEqual(0, ledger.Today);
        Assert.AreEqual(120, ledger.Held);
        Assert.AreEqual(120, ledger.Lifetime);
        Assert.IsFalse(ledger.MeetsQuota(120));
    }
}
