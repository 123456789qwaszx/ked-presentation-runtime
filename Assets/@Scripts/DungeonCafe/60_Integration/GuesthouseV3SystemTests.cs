using System.Collections.Generic;
using NUnit.Framework;
using Yarn.Unity;

/// <summary>
/// v3 2단계 이후 시스템 검증: 상태·진행 규칙·세이브·전체 캠페인 결정론.
/// 1단계 순수 계산은 GuesthouseV3RulesTests 가 담당한다.
/// 주의: 이 파일은 헤드리스 캠페인을 돌리므로 테스트 asmdef 에 YarnSpinner 참조가 필요하다.
/// </summary>
public sealed class GuesthouseV3SystemTests
{
    private static GuesthouseTuningV3 Standard => GuesthouseTuningV3.CreateStandard();

    private static CampaignStateV3 NewCampaign(ulong seed = 42UL)
        => new(GuesthouseV3Content.Build(), Standard, seed);

    // ------------------------------------------------------------
    // 게이지 0~200 (§1, §3)
    // ------------------------------------------------------------

    [Test]
    public void Gauge_HardCapsAt200_AndTracksHighestAxis()
    {
        var gauge = new MaidGaugeState(200);
        gauge.Add(BurdenAxis.Physical, 150);
        gauge.Add(BurdenAxis.Physical, 100);

        Assert.AreEqual(200, gauge.Get(BurdenAxis.Physical));

        gauge.Add(BurdenAxis.Mental, 30);
        BurdenAxis highest = gauge.HighestAxis(out int value);
        Assert.AreEqual(BurdenAxis.Physical, highest);
        Assert.AreEqual(200, value);
    }

    [Test]
    public void Gauge_FindsAxisAtOrAbove100()
    {
        var gauge = new MaidGaugeState(200);
        gauge.Add(BurdenAxis.Empathic, 100);

        Assert.IsTrue(gauge.TryFindAxisAtOrAbove(100, out BurdenAxis axis));
        Assert.AreEqual(BurdenAxis.Empathic, axis);
    }

    // ------------------------------------------------------------
    // 완전 붕괴·생환권 (§5)
    // ------------------------------------------------------------

    [Test]
    public void TotalCollapse_FirstUsesTicket_SecondLosesMaid()
    {
        CampaignStateV3 campaign = NewCampaign();
        MaidStateV3 maid = campaign.Maids[0];
        SpeciesProtocolV3 protocol = campaign.Content.GetProtocol(MonsterSpecies.ParasiticEquipment);

        maid.Gauge.SetValue(BurdenAxis.Physical, 200);
        TotalCollapseOutcome first = TotalCollapseRule.Resolve(maid, protocol, Standard);

        Assert.IsTrue(first.Rescued);
        Assert.IsFalse(maid.HasRescueTicket);
        Assert.AreEqual(100, maid.Gauge.Get(BurdenAxis.Physical), "생환 시 100 복귀");
        Assert.IsFalse(maid.IsLost);

        maid.Gauge.SetValue(BurdenAxis.Physical, 200);
        TotalCollapseOutcome second = TotalCollapseRule.Resolve(maid, protocol, Standard);

        Assert.IsFalse(second.Rescued);
        Assert.IsTrue(maid.IsLost);
    }

    // ------------------------------------------------------------
    // 관계·이해도·가게 (§8, §12)
    // ------------------------------------------------------------

    [Test]
    public void Relation_StageThresholds_0_6_14_27()
    {
        Assert.AreEqual(1, RelationRule.ResolveStage(5, Standard));
        Assert.AreEqual(2, RelationRule.ResolveStage(6, Standard));
        Assert.AreEqual(3, RelationRule.ResolveStage(14, Standard));
        Assert.AreEqual(3, RelationRule.ResolveStage(26, Standard));
        Assert.AreEqual(4, RelationRule.ResolveStage(27, Standard));
    }

    [Test]
    public void Understanding_Tiers_2_5_9()
    {
        CampaignStateV3 campaign = NewCampaign();
        UnderstandingState u = campaign.Understanding;

        Assert.AreEqual(UnderstandingTier.Unknown, u.GetTier("mon_bladeframe", Standard));
        u.AddPoints("mon_bladeframe", 2);
        Assert.AreEqual(UnderstandingTier.Partial, u.GetTier("mon_bladeframe", Standard));
        u.AddPoints("mon_bladeframe", 3);
        Assert.AreEqual(UnderstandingTier.Advanced, u.GetTier("mon_bladeframe", Standard));
        u.AddPoints("mon_bladeframe", 4);
        Assert.AreEqual(UnderstandingTier.Complete, u.GetTier("mon_bladeframe", Standard));
    }

    [Test]
    public void ShopLevel_ThresholdTable()
    {
        Assert.AreEqual(1, ShopLevelRule.Resolve(399, Standard));
        Assert.AreEqual(2, ShopLevelRule.Resolve(400, Standard));
        Assert.AreEqual(6, ShopLevelRule.Resolve(3300, Standard));
        Assert.AreEqual(7, ShopLevelRule.Resolve(4300, Standard));
    }

    [Test]
    public void OneTimeFlags_ClaimOnlyOnce()
    {
        CampaignStateV3 campaign = NewCampaign();

        Assert.IsTrue(campaign.Understanding.TryClaimOneTime("special", "mon_a", "maid_x"));
        Assert.IsFalse(campaign.Understanding.TryClaimOneTime("special", "mon_a", "maid_x"));
        Assert.IsTrue(campaign.Understanding.TryClaimOneTime("special", "mon_a", "maid_y"),
            "1회성 플래그는 개체×메이드 단위 (§4.4)");
    }

    // ------------------------------------------------------------
    // 기벽·능력 (§10, §11)
    // ------------------------------------------------------------

    [Test]
    public void QuirkSlots_AccidentEvictsWhenFull()
    {
        CampaignStateV3 campaign = NewCampaign();
        MaidStateV3 maid = campaign.Maids[0];

        Assert.IsTrue(maid.AddQuirk("qk_shion_blade", isAccident: false));
        Assert.IsTrue(maid.AddQuirk("qk_shion_silence", isAccident: false));
        Assert.IsTrue(maid.AddQuirk("qk_acc_immersion", isAccident: true));

        Assert.IsFalse(maid.AddQuirk("qk_arie_record", isAccident: false), "만석 + 안정 기벽 = 거부");
        Assert.IsTrue(maid.AddQuirk("qk_acc_nightowl", isAccident: true), "만석 + 사고성 = 안정 축출");
        Assert.AreEqual(3, maid.QuirkIds.Count);
        Assert.IsTrue(maid.HasQuirkId("qk_acc_nightowl"));
    }

    [Test]
    public void Ability_KnowledgeAndRelationGate()
    {
        CampaignStateV3 campaign = NewCampaign();
        PlayerAbilityDefinition reroll = campaign.Content.GetAbility("ab_reroll");

        campaign.Ledger.Earn(500);   // Lv2 도달 (400)

        Assert.IsFalse(AbilityRules.MeetsConditions(campaign, reroll), "일부 파악 1개체 필요");
        campaign.Understanding.AddPoints("mon_bladeframe", 2);
        Assert.IsTrue(AbilityRules.MeetsConditions(campaign, reroll));

        Assert.IsTrue(AbilityRules.TryPurchase(campaign, reroll));
        Assert.AreEqual(350, campaign.Ledger.Held);
        Assert.IsFalse(AbilityRules.TryPurchase(campaign, reroll), "중복 구매 불가");

        PlayerAbilityDefinition maidAbility = campaign.Content.GetAbility("ab_shion_repeat");
        Assert.IsFalse(AbilityRules.MeetsConditions(campaign, maidAbility));
        campaign.Maids[0].AddRelation(6, RelationDirection.Trust);   // 2단계
        Assert.IsTrue(AbilityRules.MeetsConditions(campaign, maidAbility));
    }

    [Test]
    public void QuirkResolver_NeglectChancesOverride()
    {
        CampaignStateV3 campaign = NewCampaign();
        MaidStateV3 maid = campaign.Maids[0];
        maid.AddQuirk("qk_acc_nightowl", isAccident: true);

        NeglectRule.NeglectChances chances =
            QuirkEffectResolver.NeglectChances(campaign.Content, maid, Standard);

        Assert.AreEqual(50, chances.HoldPercent);
        Assert.AreEqual(35, chances.SelfReleasePercent);
    }

    // ------------------------------------------------------------
    // 엔딩 (§15)
    // ------------------------------------------------------------

    [Test]
    public void Ending_ImmediateAndFinal()
    {
        CampaignStateV3 campaign = NewCampaign();

        Assert.AreEqual(EndingKindV3.None, EndingResolverV3.ResolveImmediate(campaign));

        campaign.BankruptcyCount = 3;
        Assert.AreEqual(EndingKindV3.Bankruptcy, EndingResolverV3.ResolveImmediate(campaign));

        campaign.BankruptcyCount = 0;
        campaign.Ledger.Earn(7000);
        campaign.Maids[0].AddRelation(27, RelationDirection.Trust);
        Assert.AreEqual(EndingKindV3.FullHouseMorning, EndingResolverV3.ResolveCampaignEnd(campaign));
    }

    // ------------------------------------------------------------
    // 세이브 (§14, §16)
    // ------------------------------------------------------------

    [Test]
    public void Save_CaptureBlockedDuringService()
    {
        CampaignStateV3 campaign = NewCampaign();
        campaign.Phase = CampaignPhaseV3.InService;

        Assert.IsFalse(GuesthouseV3SaveModel.TryCapture(campaign, out _));

        campaign.Phase = CampaignPhaseV3.SlotBoundary;
        Assert.IsTrue(GuesthouseV3SaveModel.TryCapture(campaign, out _));
    }

    [Test]
    public void Save_RoundTrip_PreservesCoreState()
    {
        CampaignStateV3 campaign = NewCampaign(777UL);
        campaign.CurrentDayNumber = 5;
        campaign.BankruptcyCount = 1;
        campaign.Ledger.Earn(300);
        campaign.Ledger.EarnNight(40);
        campaign.Ledger.TrySpend(100);
        campaign.Rng.RollDie99();

        MaidStateV3 maid = campaign.Maids[1];
        maid.Gauge.SetValue(BurdenAxis.Mental, 137);
        maid.AddRelation(9, RelationDirection.Depend);
        maid.AddQuirk("qk_arie_record", isAccident: false);
        maid.AddAftereffect(campaign.Content.GetAftereffect("se_brand"));
        maid.GetMastery(BurdenAxis.Mental).AddExperience(150);
        campaign.Understanding.AddPoints("mon_archivist", 6);
        campaign.Abilities.Grant("ab_reroll");

        Assert.IsTrue(GuesthouseV3SaveModel.TryCapture(campaign, out GuesthouseV3SaveModel save));

        CampaignStateV3 restored = save.Restore(GuesthouseV3Content.Build(), Standard);

        Assert.AreEqual(5, restored.CurrentDayNumber);
        Assert.AreEqual(1, restored.BankruptcyCount);
        Assert.AreEqual(300, restored.Ledger.Today);
        Assert.AreEqual(240, restored.Ledger.Held);
        Assert.AreEqual(340, restored.Ledger.Lifetime);
        Assert.AreEqual(campaign.Rng.State, restored.Rng.State);

        MaidStateV3 restoredMaid = restored.GetMaid(maid.MaidId);
        Assert.AreEqual(137, restoredMaid.Gauge.Get(BurdenAxis.Mental));
        Assert.AreEqual(9, restoredMaid.RelationPoints);
        Assert.IsTrue(restoredMaid.HasQuirkId("qk_arie_record"));
        Assert.IsNotNull(restoredMaid.FindAftereffect("se_brand"));
        Assert.AreEqual(150, restoredMaid.GetMastery(BurdenAxis.Mental).Experience);
        Assert.AreEqual(6, restored.Understanding.GetPoints("mon_archivist"));
        Assert.IsTrue(restored.Abilities.Owns("ab_reroll"));

        // 복원 후 다음 굴림 동일 (§14)
        Assert.AreEqual(campaign.Rng.RollDie99(), restored.Rng.RollDie99());

        campaign.ReleaseCounters();
        restored.ReleaseCounters();
    }

    // ------------------------------------------------------------
    // 전체 캠페인: 헤드리스 결정론 + 지표 (§17)
    // ------------------------------------------------------------

    [Test]
    public void HeadlessCampaign_SameSeed_IsDeterministic()
    {
        (EndingKindV3 ending, int lifetime, int commits) Run()
        {
            var campaign = NewCampaign(20260727UL);
            var screens = new HeadlessV3Screens(HeadlessPolicyV3.Ideal);
            YarnTask<EndingKindV3> task = new CampaignFlowV3(
                campaign, screens, new HeadlessNodePlayerV3()).RunAsync();

            var result = (campaign.Ending, campaign.Ledger.Lifetime, campaign.CommitLog.Entries.Count);
            campaign.ReleaseCounters();
            return result;
        }

        (EndingKindV3, int, int) first = Run();
        (EndingKindV3, int, int) second = Run();

        Assert.AreEqual(first, second, "같은 시드 = 같은 결말·수입·커밋 수 (§0.1)");
    }

    [Test]
    public void HeadlessCampaign_CompletesAndEarns()
    {
        var campaign = NewCampaign(20260727UL);
        var screens = new HeadlessV3Screens(HeadlessPolicyV3.Ideal);
        YarnTask<EndingKindV3> task = new CampaignFlowV3(
            campaign, screens, new HeadlessNodePlayerV3()).RunAsync();

        Assert.AreEqual(CampaignPhaseV3.Finished, campaign.Phase);
        Assert.AreNotEqual(EndingKindV3.None, campaign.Ending);
        Assert.Greater(screens.ServiceCount, 20, "15일 캠페인 접객 수");
        Assert.Greater(campaign.Ledger.Lifetime, 1000);
        Assert.Greater(campaign.CommitLog.Entries.Count, 50, "판정 커밋 로그 적재");

        campaign.ReleaseCounters();
    }

    [Test]
    public void HeadlessBatch_MeetsRegressionTargets()
    {
        GuesthouseV3HeadlessValidator.Report report =
            GuesthouseV3HeadlessValidator.Run(HeadlessPolicyV3.Ideal, 30, 1000UL);

        Assert.That(report.CompletionRate, Is.GreaterThanOrEqualTo(0.8),
            "완주율 (30시드 소표본이라 0.8 하한)\n" + report.ToText());
        Assert.That(report.LandingRate, Is.InRange(0.25, 0.65), report.ToText());
    }
}
