using System;
using System.Text;

/// <summary>
/// v3 규칙 핵심 불변식의 자가 검증.
///
/// NUnit/asmdef 구성과 무관하게 어디서든 호출할 수 있는 안전망이다.
/// (프로젝트가 Assembly-CSharp 단일 구성이면 EditMode 테스트 어셈블리가
///  게임 코드를 참조할 수 없으므로, 그 경우 이쪽이 1차 검증 경로가 된다.)
///
/// 사용:
///   string report = GuesthouseV3RuleSelfCheck.RunAll(out bool allPassed);
/// 에디터 메뉴, 헤드리스 부트, 콘솔 커맨드 어디에 물려도 된다.
/// 전수 케이스는 Tests/EditMode/GuesthouseV3RulesTests.cs 가 담당하고,
/// 여기는 문서 예시값과 구조 불변식만 본다.
/// </summary>
public static class GuesthouseV3RuleSelfCheck
{
    public static string RunAll(out bool allPassed)
    {
        var report = new StringBuilder();
        int failed = 0;

        GuesthouseTuningV3 tuning = GuesthouseTuningV3.CreateStandard();

        // ---- 심층 (§4) ----

        Check(report, ref failed, "적성 -3/점 (기본78 적성4 -> 66 치명 +33)", () =>
        {
            DepthRollResult r = DepthDiceRule.Interpret(
                78, new DepthRollInput(4), DepthBandLayout.Standard, tuning);
            return r.FinalValue == 66 && r.Band == DepthBand.Fatal && r.CollapseGain == 33;
        });

        Check(report, ref failed, "보정 총량 ±30 클램프", () =>
        {
            DepthRollResult r = DepthDiceRule.Interpret(
                50, new DepthRollInput(4, interventionModifier: -30), DepthBandLayout.Standard, tuning);
            return r.RawModifierSum == -42 && r.ClampedModifierSum == -30 && r.FinalValue == 20;
        });

        Check(report, ref failed, "상한형 효과는 클램프 이후 별도 적용", () =>
        {
            DepthRollResult r = DepthDiceRule.Interpret(
                95, new DepthRollInput(0, statusEffectModifier: 30, maxValueCap: 50),
                DepthBandLayout.Standard, tuning);
            return r.WasCapped && r.FinalValue == 50 && r.Band == DepthBand.Risky;
        });

        Check(report, ref failed, "회수 구간 가산 0 / 특수 구간 각인 신호", () =>
        {
            DepthRollResult recover = DepthDiceRule.Interpret(
                15, new DepthRollInput(0), DepthBandLayout.Standard, tuning);
            DepthRollResult special = DepthDiceRule.Interpret(
                97, new DepthRollInput(0), DepthBandLayout.Standard, tuning);
            return recover.CollapseGain == 0 && recover.OpensRecoveryWindow
                && special.InflictsBrand && special.CollapseGain == 49;
        });

        Check(report, ref failed, "구간 변형 후 최소 폭 유지", () =>
        {
            DepthBandLayout over = DepthBandLayout.Standard.ShiftRecoveryMax(+500, tuning.DepthMinBandWidth);
            int w = tuning.DepthMinBandWidth;
            return over.RiskyMax - over.RecoveryMax >= w
                && over.FatalMax - over.RiskyMax >= w
                && 99 - over.FatalMax >= w;
        });

        // ---- 낮 부하 (§2.3) ----

        Check(report, ref failed, "부하 공식 (굴림20 +2 -적성8 -숙련1 = 13)", () =>
        {
            LoadJudgmentResult r = LoadRangeJudgmentRule.Interpret(20, 2, 4, 1, tuning);
            return r.RawLoad == 22 && r.AppliedLoad == 13;
        });

        Check(report, ref failed, "최소 부하 바닥 4 + 원본 보존", () =>
        {
            LoadJudgmentResult r = LoadRangeJudgmentRule.Interpret(6, 0, 4, 0, tuning);
            return r.AppliedLoad == tuning.DayMinimumAppliedLoad && r.WasFloored && r.RawLoad == 6;
        });

        // ---- 결산 (§7.2) ----

        Check(report, ref failed, "문서 예시: 반응7 붕괴86 -> 210 / 붕괴74 -> 105", () =>
        {
            int at86 = SettlementRuleV3.Calculate(
                SettlementOutcomeKind.Normal, 7, 70, 60, 86, tuning).Energy;
            int at74 = SettlementRuleV3.Calculate(
                SettlementOutcomeKind.Normal, 7, 70, 60, 74, tuning).Energy;
            return at86 == 210 && at74 == 105;
        });

        Check(report, ref failed, "99/100 절벽 (x3.0 vs x0.5)", () =>
        {
            SettlementV3Result at99 = SettlementRuleV3.Calculate(
                SettlementOutcomeKind.Normal, 7, 70, 60, 99, tuning);
            SettlementV3Result escaped = SettlementRuleV3.Calculate(
                SettlementOutcomeKind.DepthEscape, 7, 70, 60, 100, tuning);
            return at99.Energy == 210 && escaped.Energy == 35;
        });

        Check(report, ref failed, "만족도 미달 -> 배율 1단 하향", () =>
        {
            SettlementV3Result r = SettlementRuleV3.Calculate(
                SettlementOutcomeKind.Normal, 5, 50, 60, 86, tuning);
            return r.WasDowngraded && Math.Abs(r.AppliedMultiplier - 1.5f) < 0.001f;
        });

        Check(report, ref failed, "완전 붕괴 결산 0", () =>
            SettlementRuleV3.Calculate(
                SettlementOutcomeKind.TotalCollapse, 9, 90, 60, 200, tuning).Energy == 0);

        // ---- 방치 (§6.2) ----

        Check(report, ref failed, "방치 60/25/15 경계", () =>
        {
            var chances = NeglectRule.NeglectChances.From(tuning);
            NeglectJudgment hold = NeglectRule.Judge(
                new FixedDiceSource(60), 85, false, chances, tuning);
            NeglectJudgment self = NeglectRule.Judge(
                new FixedDiceSource(61, 100), 85, false, chances, tuning);
            NeglectJudgment incident = NeglectRule.Judge(
                new FixedDiceSource(86), 85, false, chances, tuning);
            return hold.Outcome == NeglectCollapseOutcome.DangerHold
                && self.Outcome == NeglectCollapseOutcome.SelfRelease
                && self.CollapseAfter == tuning.NeglectSelfReleaseTargetCollapse
                && incident.Outcome == NeglectCollapseOutcome.NightIncident
                && incident.IncidentDepthBeats == tuning.NightIncidentDepthBeats;
        });

        // ---- 3장부 (§7.1) ----

        Check(report, ref failed, "3장부 계약 (Earn 동시 / EarnNight 오늘 제외 / Spend 보유만)", () =>
        {
            var ledger = new DesireLedger();
            ledger.Earn(100);
            ledger.EarnNight(40);
            bool spent = ledger.TrySpend(120);
            bool shortRejected = !ledger.TrySpend(999);
            return spent && shortRejected
                && ledger.Today == 100 && ledger.Held == 20 && ledger.Lifetime == 140;
        });

        // ---- 판정 커밋 (§14) ----

        Check(report, ref failed, "난수 상태 복원 연속성", () =>
        {
            var source = new DeterministicRng(777UL);
            source.RollDie99();
            ulong committed = source.State;
            int expected = source.RollDie99();

            var restored = new DeterministicRng(0UL);
            restored.RestoreState(committed);
            return restored.RollDie99() == expected;
        });

        // ---- 통계 새니티 (§4.3 / §17) ----

        Check(report, ref failed, "심층 평균 체류 2~5비트 (시드 고정 500회)", () =>
        {
            var rng = new DeterministicRng(20260727UL);
            var input = new DepthRollInput(0);
            int total = 0;
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
                        break;

                    collapse += roll.CollapseGain;
                }

                total += beats;
            }

            double average = (double)total / trials;
            return average >= 2.0 && average <= 5.0;
        });

        allPassed = failed == 0;
        report.Insert(0, $"GuesthouseV3 자가 검증 - {(allPassed ? "전체 통과" : $"{failed}건 실패")}\n");

        return report.ToString();
    }

    private static void Check(StringBuilder report, ref int failed, string name, Func<bool> check)
    {
        bool passed;

        try
        {
            passed = check();
        }
        catch (Exception e)
        {
            passed = false;
            report.AppendLine($"  [예외] {name}: {e.GetType().Name} {e.Message}");
        }

        if (!passed)
            failed++;

        report.AppendLine($"  [{(passed ? "통과" : "실패")}] {name}");
    }
}
