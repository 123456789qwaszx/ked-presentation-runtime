using System.Collections.Generic;

/// <summary>
/// v3 코드 내장 콘텐츠. guesthouse_design_v3.md §1, §9~§13 표를 그대로 데이터화한다.
/// SO 저작 파이프라인 도입 시 이 파일이 폴백으로 남는다.
/// </summary>
public static class GuesthouseV3Content
{
    public static GuesthouseV3ContentDB Build()
    {
        return new GuesthouseV3ContentDB(
            BuildMaids(), BuildMonsters(), BuildProtocols(),
            BuildAftereffects(), BuildQuirks(), BuildAbilities(), BuildCalendar());
    }

    // ---- 메이드 (§12.1) ----
    private static List<MaidProfileV3> BuildMaids() => new()
    {
        new("maid_shion", "시온", 1, new AxisTriple(4, 2, 1), "guarded",
            traitRecoveryShift: 8, traitRiskyShift: 0, statusModifierPercent: 100,
            dispositionChancePercent: 30, dispositionKey: "training"),
        new("maid_arie", "아리에", 2, new AxisTriple(1, 4, 2), "clinical",
            traitRecoveryShift: 0, traitRiskyShift: 0, statusModifierPercent: 50,
            dispositionChancePercent: 25, dispositionKey: "archiving"),
        new("maid_rui", "루이", 4, new AxisTriple(2, 1, 4), "dependent",
            traitRecoveryShift: 0, traitRiskyShift: -3, statusModifierPercent: 100,
            dispositionChancePercent: 35, dispositionKey: "greeting"),
    };

    // ---- 몬스터 (§13.2~13.3) ----
    private static List<ServiceBeatV3> Beats(string monsterId, BurdenAxis axis, string heavyTag)
    {
        var beats = new List<ServiceBeatV3>(3);
        for (int i = 1; i <= 3; i++)
        {
            beats.Add(new ServiceBeatV3(
                $"Beat_{monsterId}_{i}",
                new List<ServiceOptionV3>
                {
                    new(OptionIntensity.Light, axis, $"Beat_{monsterId}_{i}_light"),
                    new(OptionIntensity.Medium, axis, $"Beat_{monsterId}_{i}_medium", heavyTag),
                    new(OptionIntensity.Heavy, axis, $"Beat_{monsterId}_{i}_heavy", heavyTag),
                }));
        }
        return beats;
    }

    private static DepthActionSet Depth(string id) => new(
        $"Depth_{id}_risk", $"Depth_{id}_fatal", $"Depth_{id}_special");

    private static List<MonsterProfileV3> BuildMonsters() => new()
    {
        new("mon_bladeframe", "검틀", MonsterSpecies.ParasiticEquipment, 1,
            BurdenAxis.Physical, 60, 0, false, MonsterSpecialRule.HeavyReactionEcho,
            Beats("mon_bladeframe", BurdenAxis.Physical, "weapon"), Depth("mon_bladeframe"),
            "손잡이만 남은 장비 한 점. 스스로 온다고 적혀 있음.", "Call_mon_bladeframe"),
        new("mon_armorswarm", "갑주무리", MonsterSpecies.ParasiticEquipment, 2,
            BurdenAxis.Physical, 55, 2, true, MonsterSpecialRule.TighteningGrip,
            Beats("mon_armorswarm", BurdenAxis.Physical, "weapon"), Depth("mon_armorswarm"),
            "부품 여러 점의 합동 예약. 방 하나면 충분하다고 함.", "Call_mon_armorswarm"),
        new("mon_archivist", "기록수", MonsterSpecies.MemoryDevourer, 4,
            BurdenAxis.Mental, 60, 0, false, MonsterSpecialRule.RepetitionBoredom,
            Beats("mon_archivist", BurdenAxis.Mental, ""), Depth("mon_archivist"),
            "여러 명분의 이름으로 예약을 넣었으나 전부 같은 필체.", "Call_mon_archivist"),
        new("mon_facsimile", "이면잉크", MonsterSpecies.MemoryDevourer, 8,
            BurdenAxis.Mental, 65, 1, false, MonsterSpecialRule.AxisMasquerade,
            Beats("mon_facsimile", BurdenAxis.Mental, ""), Depth("mon_facsimile"),
            "예약서 뒷면에 다른 문장이 배어 있음.", "Call_mon_facsimile"),
        new("mon_resonance", "잔향", MonsterSpecies.ResonanceAmplifier, 6,
            BurdenAxis.Empathic, 55, 0, false, MonsterSpecialRule.Reverb,
            Beats("mon_resonance", BurdenAxis.Empathic, "echo"), Depth("mon_resonance"),
            "조용한 방을 요청. 소리를 내지 않겠다고 여러 번 강조함.", "Call_mon_resonance"),
        new("mon_beacon", "공명등", MonsterSpecies.ResonanceAmplifier, 9,
            BurdenAxis.Empathic, 60, 1, false, MonsterSpecialRule.DangerCraving,
            Beats("mon_beacon", BurdenAxis.Empathic, "echo"), Depth("mon_beacon"),
            "밤에만 체크인하겠다는 조건. 불을 끄지 말 것.", "Call_mon_beacon"),
        new("mon_coilbed", "감김상", MonsterSpecies.PredatoryBinder, 11,
            BurdenAxis.Physical, 65, 2, false, MonsterSpecialRule.Overstay,
            Beats("mon_coilbed", BurdenAxis.Physical, ""), Depth("mon_coilbed"),
            "장기 투숙 문의. 체크아웃 일자를 적지 않음.", "Call_mon_coilbed"),
        new("mon_sinker", "침면", MonsterSpecies.PredatoryBinder, 13,
            BurdenAxis.Physical, 70, 3, false, MonsterSpecialRule.OverstayVeil,
            Beats("mon_sinker", BurdenAxis.Physical, ""), Depth("mon_sinker"),
            "가장 깊은 방을 요구. 침구는 본인이 지참한다고 함.", "Call_mon_sinker"),
    };

    // ---- 종족 규약 (§13.1) ----
    private static List<SpeciesProtocolV3> BuildProtocols() => new()
    {
        new(MonsterSpecies.ParasiticEquipment, "기생 장비종",
            "ControlLoss_ParasiticEquipment", "Ending_Collapse_ParasiticEquipment"),
        new(MonsterSpecies.MemoryDevourer, "기억 포식종",
            "ControlLoss_MemoryDevourer", "Ending_Collapse_MemoryDevourer"),
        new(MonsterSpecies.ResonanceAmplifier, "감응 증폭종",
            "ControlLoss_ResonanceAmplifier", "Ending_Collapse_ResonanceAmplifier"),
        new(MonsterSpecies.PredatoryBinder, "포식/구속종",
            "ControlLoss_PredatoryBinder", "Ending_Collapse_PredatoryBinder"),
    };

    // ---- 후유증 (§9) ----
    private static List<AftereffectDefinition> BuildAftereffects() => new()
    {
        // 떨림은 배정을 막지 않는다 - 막으면 풀 고갈로 후반 경제가 연쇄 붕괴 (시뮬 검증).
        // 대신 부하 판정 +2 로 '떨리는 손' 을 표현한다.
        new("se_tremor", "떨림", blocksAssignment: false, blockDays: 0,
            careCuresNeeded: 1, neglectHealDays: 2, depthDieModifier: 0,
            MonsterSpecies.None, permanentizeQuirkId: null, penalizesRelationWhenNeglected: false,
            dayLoadPenalty: 2),
        new("se_brand", "각인", blocksAssignment: true, blockDays: 1,
            careCuresNeeded: 2, neglectHealDays: 0, depthDieModifier: 7,
            MonsterSpecies.None, permanentizeQuirkId: "qk_acc_brand", penalizesRelationWhenNeglected: false),
        new("se_hollow", "공동", blocksAssignment: true, blockDays: 0,
            careCuresNeeded: 3, neglectHealDays: 0, depthDieModifier: 0,
            MonsterSpecies.None, permanentizeQuirkId: null, penalizesRelationWhenNeglected: true),
    };

    // ---- 기벽 (§10). TaggedSpecies.None = 전 종족. ----
    private static List<QuirkDefinition> BuildQuirks() => new()
    {
        new("qk_shion_blade", "칼끝 예절", "maid_shion", false,
            QuirkEffectKind.MediumReactionUpgrade, 0, 0, MonsterSpecies.None, BurdenAxis.Physical,
            "weapon", "dlg_shion_blade", "Night_Request_maid_shion_qk_shion_blade"),
        new("qk_shion_silence", "침묵 훈련", "maid_shion", false,
            QuirkEffectKind.RiskyFloorShift, 4, 0, MonsterSpecies.None, BurdenAxis.Physical,
            null, "dlg_shion_silence", "Night_Request_maid_shion_qk_shion_silence"),
        new("qk_arie_record", "기록 습관", "maid_arie", false,
            QuirkEffectKind.UnderstandingOnSettle, 1, 0, MonsterSpecies.None, BurdenAxis.Mental,
            null, "dlg_arie_record", "Night_Request_maid_arie_qk_arie_record"),
        new("qk_arie_distance", "임상적 거리", "maid_arie", false,
            QuirkEffectKind.LoadRollTakeLowest, 0, 0, MonsterSpecies.None, BurdenAxis.Mental,
            null, "dlg_arie_distance", "Night_Request_maid_arie_qk_arie_distance"),
        new("qk_rui_echo", "따라 부르기", "maid_rui", false,
            QuirkEffectKind.MediumReactionUpgrade, 0, 0, MonsterSpecies.None, BurdenAxis.Empathic,
            "echo", "dlg_rui_echo", "Night_Request_maid_rui_qk_rui_echo"),
        new("qk_rui_nap", "곁잠", "maid_rui", false,
            QuirkEffectKind.NeglectRecoveryBonus, 6, 0, MonsterSpecies.None, BurdenAxis.Empathic,
            null, "dlg_rui_nap", "Night_Request_maid_rui_qk_rui_nap"),
        new("qk_acc_brand", "각인 잔향", null, true,
            QuirkEffectKind.SpeciesBrandEcho, 7, 0, MonsterSpecies.None, BurdenAxis.Physical,
            null, "dlg_acc_brand", "Night_Request_any_qk_acc_brand"),
        new("qk_acc_immersion", "과몰입", null, true,
            QuirkEffectKind.OverImmersion, 5, 20, MonsterSpecies.None, BurdenAxis.Physical,
            null, "dlg_acc_immersion", "Night_Request_any_qk_acc_immersion"),
        new("qk_acc_depend", "의존 형성", null, true,
            QuirkEffectKind.DependencyForming, 5, 0, MonsterSpecies.None, BurdenAxis.Empathic,
            null, "dlg_acc_depend", "Night_Request_any_qk_acc_depend"),
        new("qk_acc_nightowl", "밤샘 버릇", null, true,
            QuirkEffectKind.NeglectChancesOverride, 50, 35, MonsterSpecies.None, BurdenAxis.Mental,
            null, "dlg_acc_nightowl", "Night_Request_any_qk_acc_nightowl"),
        new("qk_acc_hollowmark", "공동의 흔적", null, true,
            QuirkEffectKind.HollowMark, 5, 4, MonsterSpecies.None, BurdenAxis.Physical,
            null, "dlg_acc_hollowmark", "Night_Request_any_qk_acc_hollowmark"),
    };

    // ---- 능력 (§11) ----
    private static List<PlayerAbilityDefinition> BuildAbilities() => new()
    {
        new("ab_reroll", "재굴림", AbilityEffectKind.DepthReroll, 0,
            AbilityUseLimit.PerDay, 1, 2, 150, KnowledgeGateKind.AnyPartialCount, 1, ResearchType.Physical),
        new("ab_minus10", "진정 신호", AbilityEffectKind.DepthDelta, -10,
            AbilityUseLimit.PerDay, 1, 2, 220, KnowledgeGateKind.AnyCompleteCount, 1, ResearchType.Physical),
        new("ab_downgrade", "결과 하향", AbilityEffectKind.DepthBandDowngrade, 0,
            AbilityUseLimit.PerDay, 1, 4, 320, KnowledgeGateKind.AnyCompleteCount, 2, ResearchType.Physical),
        new("ab_cap50", "제압 유도", AbilityEffectKind.DepthMaxCap, 50,
            AbilityUseLimit.PerDay, 1, 4, 350, KnowledgeGateKind.DepthWitnessCount, 3, ResearchType.Physical),
        new("ab_forcewindow", "회수 구간 생성", AbilityEffectKind.ForceRecoveryWindow, 0,
            AbilityUseLimit.PerCampaign, 2, 5, 500, KnowledgeGateKind.DepthWitnessCount, 5, ResearchType.Physical),
        new("ab_ph_disperse", "압력 분산", AbilityEffectKind.LoadRedirectPercent, 40,
            AbilityUseLimit.PerDay, 1, 3, 200, KnowledgeGateKind.TypeServiceCount, 6, ResearchType.Physical),
        new("ab_ph_limit", "충격 제한", AbilityEffectKind.LoadCap, 12,
            AbilityUseLimit.PerDay, 1, 3, 260, KnowledgeGateKind.TypeCompleteCount, 1, ResearchType.Physical),
        new("ab_ph_reveal", "위험 구간 공개", AbilityEffectKind.RevealDepthTable, 0,
            AbilityUseLimit.Passive, 0, 2, 140, KnowledgeGateKind.TypeWitnessCount, 1, ResearchType.Physical),
        new("ab_mn_read", "징후 판독", AbilityEffectKind.PredictBand, 0,
            AbilityUseLimit.PerDay, 2, 2, 160, KnowledgeGateKind.TypeServiceCount, 4, ResearchType.Mental),
        new("ab_mn_seal", "결과 봉인", AbilityEffectKind.SealSpecialResult, 0,
            AbilityUseLimit.PerDay, 1, 4, 300, KnowledgeGateKind.TypeCompleteCount, 1, ResearchType.Mental),
        new("ab_mn_block", "자극 차단", AbilityEffectKind.NegateMonsterMods, 0,
            AbilityUseLimit.PerDay, 1, 4, 280, KnowledgeGateKind.TypeWitnessCount, 2, ResearchType.Mental),
        new("ab_em_induce", "충동 유도", AbilityEffectKind.ReactionUpgradePlusLoad, 6,
            AbilityUseLimit.PerDay, 1, 3, 240, KnowledgeGateKind.TypeServiceCount, 4, ResearchType.Empathic),
        new("ab_em_convert", "반응 전환", AbilityEffectKind.ConvertLoadToReaction, 2,
            AbilityUseLimit.PerDay, 1, 4, 300, KnowledgeGateKind.TypeCompleteCount, 1, ResearchType.Empathic),
        new("ab_em_dampen", "감응 차단", AbilityEffectKind.DepthDelta, -8,
            AbilityUseLimit.PerDay, 1, 5, 320, KnowledgeGateKind.TypeWitnessCount, 2, ResearchType.Empathic),
        new("ab_shion_repeat", "명령을 반복한다", AbilityEffectKind.MaidDepthReroll, 0,
            AbilityUseLimit.PerService, 1, 1, 0, KnowledgeGateKind.None, 0, ResearchType.Physical,
            "maid_shion", 2),
        new("ab_shion_carry", "끝까지 데려온다", AbilityEffectKind.StopAt199, 0,
            AbilityUseLimit.PerCampaign, 1, 1, 0, KnowledgeGateKind.None, 0, ResearchType.Physical,
            "maid_shion", 3),
        new("ab_arie_fact", "사실을 읽어준다", AbilityEffectKind.MaidPredictMinus, -5,
            AbilityUseLimit.PerDay, 1, 1, 0, KnowledgeGateKind.None, 0, ResearchType.Mental,
            "maid_arie", 2),
        new("ab_arie_refuse", "말하지 않은 거절", AbilityEffectKind.RemoveWorstAction, 0,
            AbilityUseLimit.PerService, 1, 1, 0, KnowledgeGateKind.None, 0, ResearchType.Mental,
            "maid_arie", 3),
        new("ab_rui_name", "이름을 부른다", AbilityEffectKind.MaidRecoveryShift, 8,
            AbilityUseLimit.Passive, 0, 1, 0, KnowledgeGateKind.None, 0, ResearchType.Empathic,
            "maid_rui", 2),
        new("ab_rui_promise", "약속을 상기시킨다", AbilityEffectKind.AutoCatchRecovery, 0,
            AbilityUseLimit.Passive, 0, 1, 0, KnowledgeGateKind.None, 0, ResearchType.Empathic,
            "maid_rui", 3),
    };

    // ---- 캘린더 (§1) ----
    private static List<CampaignDayPlan> BuildCalendar() => new()
    {
        // 시뮬 확정 곡선: 1주 완만 -> 2주 상승 -> 10일차부터 280 고원 (침면/감김상 주간 압박은 개체가 담당)
        new(1, 2, 100), new(2, 3, 120), new(3, 3, 130), new(4, 3, 140), new(5, 3, 150),
        new(6, 3, 170), new(7, 3, 200), new(8, 3, 230), new(9, 3, 255), new(10, 3, 280),
        new(11, 3, 280), new(12, 3, 280), new(13, 3, 280), new(14, 3, 280), new(15, 3, 280),
    };
}
