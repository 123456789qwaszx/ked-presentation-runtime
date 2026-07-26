using System.Collections.Generic;

/// <summary>
/// 버티컬 슬라이스용 코드 내장 콘텐츠.
///
/// SO 저작 파이프라인이 준비되기 전에도 루프 전체가 돌아가도록 최소 분량만 정의한다.
///   메이드 3, 몬스터 4(종족별 1), 시나리오 4(비트 3 + 분기 1)
/// 실제 저작은 GuesthouseContentBundleSO 쪽으로 옮기고 이 파일은 폴백으로만 남기면 된다.
///
/// 노드 이름은 전부 규칙적으로 붙어 있으므로, Yarn 쪽에 같은 이름의 노드만 만들면 바로 연결된다.
/// </summary>
public static class GuesthouseDemoContent
{
    public static GuesthouseContentDB Build()
    {
        List<ServiceScenario> scenarios = new();

        List<MonsterProfile> monsters = BuildMonsters();

        scenarios.Add(BuildBladeframeScenario());
        scenarios.Add(BuildArchivistScenario());
        scenarios.Add(BuildResonanceScenario());
        scenarios.Add(BuildCoilbedScenario());

        return new GuesthouseContentDB(
            ProgressionTuning.CreateDefault(),
            BuildMaids(),
            monsters,
            scenarios,
            BuildProtocols());
    }

    // ------------------------------------------------------------
    // Maids
    // ------------------------------------------------------------
    private static List<MaidProfile> BuildMaids()
    {
        return new List<MaidProfile>
        {
            new(
                "maid_shion",
                "시온",
                aptitude: new AxisTriple(4, 2, 1),
                collapseLimit: AxisTriple.Uniform(100),
                proposalStyleKey: "guarded",
                traitKeys: new[] { "무기취급", "신중" }),

            new(
                "maid_arie",
                "아리에",
                aptitude: new AxisTriple(1, 4, 2),
                collapseLimit: AxisTriple.Uniform(100),
                proposalStyleKey: "clinical",
                traitKeys: new[] { "기록", "침착" }),

            new(
                "maid_rui",
                "루이",
                aptitude: new AxisTriple(2, 1, 4),
                collapseLimit: AxisTriple.Uniform(100),
                proposalStyleKey: "dependent",
                traitKeys: new[] { "순응", "호기심" }),
        };
    }

    // ------------------------------------------------------------
    // Monsters
    // ------------------------------------------------------------
    private static List<MonsterProfile> BuildMonsters()
    {
        return new List<MonsterProfile>
        {
            new(
                "mon_bladeframe",
                "검틀",
                MonsterSpecies.ParasiticEquipment,
                demandAxis: BurdenAxis.Physical,
                loadBias: new AxisTriple(3, 1, 0),
                requiredSatisfaction: 60,
                maxSatisfaction: 100,
                satisfactionPerScore: 10,
                scenarioKey: "sc_bladeframe",
                reservationPostText: "손잡이만 남은 장비 한 점. 스스로 온다고 적혀 있음.",
                phoneCallNodeName: "Call_mon_bladeframe",
                codexNotes: new[]
                {
                    "착용 행동에 강하게 반응한다.",
                    "무기 사용을 선호한다.",
                    "요구 유형: 육체.",
                }),

            new(
                "mon_archivist",
                "기록수",
                MonsterSpecies.MemoryDevourer,
                demandAxis: BurdenAxis.Mental,
                loadBias: new AxisTriple(0, 3, 1),
                requiredSatisfaction: 60,
                maxSatisfaction: 100,
                satisfactionPerScore: 10,
                scenarioKey: "sc_archivist",
                reservationPostText: "여러 명분의 이름으로 예약을 넣었으나 전부 같은 필체.",
                phoneCallNodeName: "Call_mon_archivist",
                codexNotes: new[]
                {
                    "질문과 대답의 반복에 반응한다.",
                    "담당자의 경력을 물어본다.",
                    "요구 유형: 정신.",
                }),

            new(
                "mon_resonance",
                "잔향",
                MonsterSpecies.ResonanceAmplifier,
                demandAxis: BurdenAxis.Empathic,
                loadBias: new AxisTriple(0, 1, 3),
                requiredSatisfaction: 55,
                maxSatisfaction: 100,
                satisfactionPerScore: 10,
                scenarioKey: "sc_resonance",
                reservationPostText: "조용한 방을 요청. 소리를 내지 않겠다고 여러 번 강조함.",
                phoneCallNodeName: "Call_mon_resonance",
                codexNotes: new[]
                {
                    "담당자의 감정을 되돌려 증폭한다.",
                    "접촉 시간이 길수록 영향이 남는다.",
                    "요구 유형: 감응.",
                }),

            new(
                "mon_coilbed",
                "감김상",
                MonsterSpecies.PredatoryBinder,
                demandAxis: BurdenAxis.Physical,
                loadBias: new AxisTriple(3, 1, 1),
                requiredSatisfaction: 65,
                maxSatisfaction: 100,
                satisfactionPerScore: 10,
                scenarioKey: "sc_coilbed",
                reservationPostText: "장기 투숙 문의. 체크아웃 일자를 적지 않음.",
                phoneCallNodeName: "Call_mon_coilbed",
                codexNotes: new[]
                {
                    "정리와 마무리 동작에 반응한다.",
                    "철수 시점을 흐린다.",
                    "요구 유형: 육체.",
                }),
        };
    }

    // ------------------------------------------------------------
    // Species protocols
    // ------------------------------------------------------------
    private static List<SpeciesProtocol> BuildProtocols()
    {
        return new List<SpeciesProtocol>
        {
            new(
                MonsterSpecies.ParasiticEquipment,
                "기생 장비종",
                controlLossNodeName: "ControlLoss_ParasiticEquipment",
                collapseEndingNodeName: "Ending_Collapse_ParasiticEquipment",
                collapseEndingKey: "Ending_Collapse_ParasiticEquipment",
                allowsWithdrawAfterControlLoss: false,
                autonomousResidualLoad: new AxisTriple(10, 4, 2),
                autonomousBeatCount: 2,
                riskNotes: new[]
                {
                    "정신 붕괴 한계 초과 시 통제 신호가 차단된다.",
                    "통제 상실 이후 즉시 철수 불가능.",
                }),

            new(
                MonsterSpecies.MemoryDevourer,
                "기억 포식종",
                controlLossNodeName: "ControlLoss_MemoryDevourer",
                collapseEndingNodeName: "Ending_Collapse_MemoryDevourer",
                collapseEndingKey: "Ending_Collapse_MemoryDevourer",
                allowsWithdrawAfterControlLoss: false,
                autonomousResidualLoad: new AxisTriple(2, 12, 2),
                autonomousBeatCount: 2,
                riskNotes: new[]
                {
                    "담당자의 역할 인식을 대체하려 한다.",
                    "통제 상실 이후 호명에 응답하지 않는다.",
                }),

            new(
                MonsterSpecies.ResonanceAmplifier,
                "감응 증폭종",
                controlLossNodeName: "ControlLoss_ResonanceAmplifier",
                collapseEndingNodeName: "Ending_Collapse_ResonanceAmplifier",
                collapseEndingKey: "Ending_Collapse_ResonanceAmplifier",
                allowsWithdrawAfterControlLoss: true,
                autonomousResidualLoad: new AxisTriple(2, 4, 10),
                autonomousBeatCount: 2,
                riskNotes: new[]
                {
                    "주입된 충동과 본인의 감정을 구분하기 어려워진다.",
                    "통제 상실 이후에도 회수 자체는 가능하다.",
                }),

            new(
                MonsterSpecies.PredatoryBinder,
                "포식·구속종",
                controlLossNodeName: "ControlLoss_PredatoryBinder",
                collapseEndingNodeName: "Ending_Collapse_PredatoryBinder",
                collapseEndingKey: "Ending_Collapse_PredatoryBinder",
                allowsWithdrawAfterControlLoss: false,
                autonomousResidualLoad: new AxisTriple(12, 3, 3),
                autonomousBeatCount: 3,
                riskNotes: new[]
                {
                    "철수 판단 시점을 스스로 인지하지 못한다.",
                    "접객이 종료되어도 담당자가 방을 나오지 않는다.",
                }),
        };
    }

    // ------------------------------------------------------------
    // Scenarios
    // ------------------------------------------------------------
    private static ServiceScenario BuildBladeframeScenario()
    {
        List<ServiceBeat> beats = new()
        {
            new ServiceBeat(
                "bf_entry",
                "Service_mon_bladeframe_Entry",
                new List<ServiceActionOption>
                {
                    Option("bf_observe", "거리를 두고 상태를 확인하겠습니다.",
                        load: new AxisTriple(4, 2, 0),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 1,
                        preferredTrait: "신중"),

                    Option("bf_draw", "검을 꺼내고 싶다는 충동이 들어요. 결정해주세요.",
                        load: new AxisTriple(12, 6, 2),
                        reaction: MonsterReactionGrade.Satisfied,
                        nextBeatKey: "bf_blade",
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 3,
                        preferredTrait: "무기취급",
                        riskWeight: 2),

                    Option("bf_touch", "손잡이에 손을 대보겠습니다.",
                        load: new AxisTriple(8, 8, 2),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 2,
                        riskWeight: 1),
                }),

            new ServiceBeat(
                "bf_blade",
                "Service_mon_bladeframe_Blade",
                new List<ServiceActionOption>
                {
                    Option("bf_blade_hold", "이대로 들고만 있겠습니다.",
                        load: new AxisTriple(10, 4, 2),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 3),

                    Option("bf_blade_swing", "한 번 휘두르라고 요구하고 있어요.",
                        load: new AxisTriple(18, 6, 2),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 4,
                        preferredTrait: "무기취급",
                        riskWeight: 3),

                    Option("bf_blade_release", "내려놓겠습니다.",
                        load: new AxisTriple(4, 6, 0),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 2),
                }),

            new ServiceBeat(
                "bf_close",
                "Service_mon_bladeframe_Close",
                new List<ServiceActionOption>
                {
                    Option("bf_close_wipe", "정비를 마무리하겠습니다.",
                        load: new AxisTriple(8, 4, 2),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 3,
                        isTerminal: true),

                    Option("bf_close_leave", "그대로 두고 나가겠습니다.",
                        load: new AxisTriple(3, 5, 0),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 1,
                        isTerminal: true),

                    Option("bf_close_wear", "장비를 착용해보고 싶어요.",
                        load: new AxisTriple(20, 14, 4),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 5,
                        riskWeight: 5,
                        isTerminal: true),
                },
                isTerminal: true),
        };

        return new ServiceScenario(
            "sc_bladeframe",
            "mon_bladeframe",
            briefingNodeName: "Service_mon_bladeframe_Briefing",
            completionNodeName: "Service_mon_bladeframe_Complete",
            beats,
            beatBudget: 4);
    }

    private static ServiceScenario BuildArchivistScenario()
    {
        List<ServiceBeat> beats = new()
        {
            new ServiceBeat(
                "ar_entry",
                "Service_mon_archivist_Entry",
                new List<ServiceActionOption>
                {
                    Option("ar_answer", "질문에 사실대로 답하겠습니다.",
                        load: new AxisTriple(0, 10, 2),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 2,
                        preferredTrait: "기록"),

                    Option("ar_deflect", "업무 범위 밖이라고 잘라 말하겠습니다.",
                        load: new AxisTriple(0, 5, 0),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 1,
                        preferredTrait: "침착"),

                    Option("ar_recite", "제 경력을 처음부터 읊으라고 해요.",
                        load: new AxisTriple(0, 16, 4),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        nextBeatKey: "ar_deep",
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 4,
                        riskWeight: 3),
                }),

            new ServiceBeat(
                "ar_deep",
                "Service_mon_archivist_Deep",
                new List<ServiceActionOption>
                {
                    Option("ar_deep_continue", "계속 이야기하겠습니다.",
                        load: new AxisTriple(0, 18, 4),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 4,
                        riskWeight: 4),

                    Option("ar_deep_pause", "잠시 끊고 기록만 남기겠습니다.",
                        load: new AxisTriple(0, 8, 2),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 2,
                        preferredTrait: "기록"),

                    Option("ar_deep_stop", "이름을 다시 확인시켜 주세요.",
                        load: new AxisTriple(0, 6, 0),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 1),
                }),

            new ServiceBeat(
                "ar_close",
                "Service_mon_archivist_Close",
                new List<ServiceActionOption>
                {
                    Option("ar_close_log", "오늘 기록을 정리해 전달하겠습니다.",
                        load: new AxisTriple(0, 10, 2),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 3,
                        isTerminal: true),

                    Option("ar_close_short", "인사만 하고 나오겠습니다.",
                        load: new AxisTriple(0, 4, 0),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 1,
                        isTerminal: true),

                    Option("ar_close_swap", "제 이름을 빌려달라고 합니다.",
                        load: new AxisTriple(0, 22, 6),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 5,
                        riskWeight: 5,
                        isTerminal: true),
                },
                isTerminal: true),
        };

        return new ServiceScenario(
            "sc_archivist",
            "mon_archivist",
            "Service_mon_archivist_Briefing",
            "Service_mon_archivist_Complete",
            beats,
            beatBudget: 4);
    }

    private static ServiceScenario BuildResonanceScenario()
    {
        List<ServiceBeat> beats = new()
        {
            new ServiceBeat(
                "rs_entry",
                "Service_mon_resonance_Entry",
                new List<ServiceActionOption>
                {
                    Option("rs_quiet", "숨을 고르고 반응을 억제하겠습니다.",
                        load: new AxisTriple(0, 4, 8),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 2,
                        preferredTrait: "순응"),

                    Option("rs_speak", "말을 걸어보겠습니다.",
                        load: new AxisTriple(0, 4, 12),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 3,
                        preferredTrait: "호기심"),

                    Option("rs_sync", "맞춰주는 편이 빠를 것 같아요.",
                        load: new AxisTriple(0, 6, 18),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        nextBeatKey: "rs_deep",
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 4,
                        riskWeight: 4),
                }),

            new ServiceBeat(
                "rs_deep",
                "Service_mon_resonance_Deep",
                new List<ServiceActionOption>
                {
                    Option("rs_deep_hold", "이 상태를 유지하겠습니다.",
                        load: new AxisTriple(0, 6, 16),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 4,
                        riskWeight: 4),

                    Option("rs_deep_step", "한 걸음 물러나겠습니다.",
                        load: new AxisTriple(0, 6, 6),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 2),

                    Option("rs_deep_cut", "차단 절차를 밟겠습니다.",
                        load: new AxisTriple(2, 8, 2),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 2),
                }),

            new ServiceBeat(
                "rs_close",
                "Service_mon_resonance_Close",
                new List<ServiceActionOption>
                {
                    Option("rs_close_bow", "정상 절차대로 마치겠습니다.",
                        load: new AxisTriple(0, 4, 10),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 3,
                        isTerminal: true),

                    Option("rs_close_quick", "바로 나오겠습니다.",
                        load: new AxisTriple(0, 4, 4),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 1,
                        isTerminal: true),

                    Option("rs_close_stay", "조금만 더 있어 달라고 해요.",
                        load: new AxisTriple(0, 8, 22),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Empathic, requiredAptitude: 5,
                        riskWeight: 5,
                        isTerminal: true),
                },
                isTerminal: true),
        };

        return new ServiceScenario(
            "sc_resonance",
            "mon_resonance",
            "Service_mon_resonance_Briefing",
            "Service_mon_resonance_Complete",
            beats,
            beatBudget: 4);
    }

    private static ServiceScenario BuildCoilbedScenario()
    {
        List<ServiceBeat> beats = new()
        {
            new ServiceBeat(
                "cb_entry",
                "Service_mon_coilbed_Entry",
                new List<ServiceActionOption>
                {
                    Option("cb_check", "체크아웃 일자를 다시 확인하겠습니다.",
                        load: new AxisTriple(4, 6, 0),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 2,
                        preferredTrait: "침착"),

                    Option("cb_tidy", "침구를 정리하겠습니다.",
                        load: new AxisTriple(12, 4, 2),
                        reaction: MonsterReactionGrade.Satisfied,
                        nextBeatKey: "cb_inner",
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 3),

                    Option("cb_enter", "안쪽까지 손이 닿아야 할 것 같아요.",
                        load: new AxisTriple(18, 6, 4),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        nextBeatKey: "cb_inner",
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 4,
                        riskWeight: 4),
                }),

            new ServiceBeat(
                "cb_inner",
                "Service_mon_coilbed_Inner",
                new List<ServiceActionOption>
                {
                    Option("cb_inner_work", "계속 정리하겠습니다.",
                        load: new AxisTriple(16, 4, 4),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 4,
                        riskWeight: 3),

                    Option("cb_inner_back", "손을 빼겠습니다.",
                        load: new AxisTriple(8, 6, 2),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 2),

                    Option("cb_inner_call", "지원을 요청하겠습니다.",
                        load: new AxisTriple(4, 8, 0),
                        reaction: MonsterReactionGrade.NoResponse,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 3),
                }),

            new ServiceBeat(
                "cb_close",
                "Service_mon_coilbed_Close",
                new List<ServiceActionOption>
                {
                    Option("cb_close_exit", "철수 절차를 밟겠습니다.",
                        load: new AxisTriple(10, 6, 2),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 3,
                        isTerminal: true),

                    Option("cb_close_report", "보고만 남기고 나오겠습니다.",
                        load: new AxisTriple(4, 4, 0),
                        reaction: MonsterReactionGrade.Satisfied,
                        requiredAxis: BurdenAxis.Mental, requiredAptitude: 2,
                        isTerminal: true),

                    Option("cb_close_stay", "조금 더 마무리하고 싶어요.",
                        load: new AxisTriple(24, 8, 6),
                        reaction: MonsterReactionGrade.GreatlySatisfied,
                        requiredAxis: BurdenAxis.Physical, requiredAptitude: 5,
                        riskWeight: 5,
                        isTerminal: true),
                },
                isTerminal: true),
        };

        return new ServiceScenario(
            "sc_coilbed",
            "mon_coilbed",
            "Service_mon_coilbed_Briefing",
            "Service_mon_coilbed_Complete",
            beats,
            beatBudget: 4);
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------
    private static ServiceActionOption Option(
        string optionKey,
        string proposalText,
        AxisTriple load,
        MonsterReactionGrade reaction,
        string nextBeatKey = null,
        BurdenAxis requiredAxis = BurdenAxis.Physical,
        int requiredAptitude = 0,
        string preferredTrait = null,
        int riskWeight = 0,
        bool isTerminal = false)
    {
        return new ServiceActionOption(
            optionKey,
            proposalText,
            approvalNodeName: $"Approve_{optionKey}",
            load,
            reaction,
            satisfactionBonus: 0,
            nextBeatKey: nextBeatKey,
            requiredAptitudeAxis: requiredAxis,
            requiredAptitude: requiredAptitude,
            preferredTraitKey: preferredTrait,
            riskWeight: riskWeight,
            isTerminalAction: isTerminal);
    }
}
