using System;

// v3 시스템 공용 enum 모음. (guesthouse_design_v3.md 절 번호 병기)

/// <summary>낮 옵션 강도 3계층. (§2.2)</summary>
public enum OptionIntensity { Light = 0, Medium = 1, Heavy = 2 }

/// <summary>관계 방향. 다수 쪽이 대사 세트와 에필로그 변형을 결정. (§12.2)</summary>
public enum RelationDirection { Trust = 0, Depend = 1 }

/// <summary>밤 직접 처리 종류. (§6.1)</summary>
public enum NightChoiceKind { None = 0, Care = 1, ManagedRelease = 2 }

/// <summary>수첩 이해도 4단계. (§8.2)</summary>
public enum UnderstandingTier { Unknown = 0, Partial = 1, Advanced = 2, Complete = 3 }

/// <summary>연구 타입 매핑. (§11.2: 신체=기생장비/포식구속 / 정신=기억포식 / 감응=감응증폭)</summary>
public enum ResearchType { Physical = 0, Mental = 1, Empathic = 2 }

/// <summary>개체 특이 규칙. (§13.2)</summary>
public enum MonsterSpecialRule
{
    None = 0,
    /// <summary>검틀: 중 옵션의 반응이 강 등급으로 산정.</summary>
    HeavyReactionEcho = 1,
    /// <summary>갑주무리: 3비트째 부하 보정 +4.</summary>
    TighteningGrip = 2,
    /// <summary>기록수: 같은 강도 연속 승인 시 반응 1단계 하향.</summary>
    RepetitionBoredom = 3,
    /// <summary>이면잉크: 이해도 고도 미만이면 요구축을 감응으로 위장 표시.</summary>
    AxisMasquerade = 4,
    /// <summary>잔향: 매 비트 적용 부하의 20%를 추가 감응 부하로 반향.</summary>
    Reverb = 5,
    /// <summary>공명등: 비트 개시 시 붕괴 80~99 이면 반응 1단계 상향.</summary>
    DangerCraving = 6,
    /// <summary>감김상: 종료 시 붕괴 ≥80 이면 부하 +6 추가 비트 1회 강제.</summary>
    Overstay = 7,
    /// <summary>침면: Overstay + 심층 첫 비트 회수 구간 무효.</summary>
    OverstayVeil = 8,
}

/// <summary>기벽 효과 종류. (§10)</summary>
public enum QuirkEffectKind
{
    None = 0,
    /// <summary>심층 회수 구간 상한 ±magnitude.</summary>
    RecoveryBandShift = 1,
    /// <summary>심층 위험 구간 하한 +magnitude (회수 편입).</summary>
    RiskyFloorShift = 2,
    /// <summary>지정 축 부하 판정 2회 굴려 낮은 값. (임상적 거리)</summary>
    LoadRollTakeLowest = 3,
    /// <summary>접객 종료 시 이해도 +magnitude. (기록 습관)</summary>
    UnderstandingOnSettle = 4,
    /// <summary>방치 자연 회복 +magnitude. (곁잠)</summary>
    NeglectRecoveryBonus = 5,
    /// <summary>중 옵션 반응을 강 등급으로. (칼끝 예절/따라 부르기 - 태그 축 한정)</summary>
    MediumReactionUpgrade = 6,
    /// <summary>방치 확률 재정의: magnitude=유지%, secondary=자기해소%. (밤샘 버릇)</summary>
    NeglectChancesOverride = 7,
    /// <summary>태그 종족 심층 주사위 +magnitude / 목격 이해도 x2. (각인 잔향)</summary>
    SpeciesBrandEcho = 8,
    /// <summary>80~99 종료 시 secondary% 확률 부하 +magnitude, 반응 +1. (과몰입)</summary>
    OverImmersion = 9,
    /// <summary>관리 붕괴 회수율 −magnitude%p, 관계 +1 추가. (의존 형성)</summary>
    DependencyForming = 10,
    /// <summary>안정 회복 −magnitude, 회수 구간 +secondary. (공동의 흔적)</summary>
    HollowMark = 11,
}

/// <summary>플레이어 능력 효과 종류. (§11)</summary>
public enum AbilityEffectKind
{
    None = 0,
    DepthReroll = 1,            // 재굴림
    DepthDelta = 2,             // 최종값 +magnitude (음수)
    DepthBandDowngrade = 3,     // 구간 한 단계 하향
    DepthMaxCap = 4,            // 최대값 magnitude
    ForceRecoveryWindow = 5,    // 다음 비트 회수 강제
    LoadRedirectPercent = 6,    // 부하 magnitude% 타 축 이전
    LoadCap = 7,                // 부하 상한 magnitude
    RevealDepthTable = 8,       // 심층 구간표 상시 공개 (패시브/정보)
    PredictBand = 9,            // 굴림 전 최빈 구간 표시 (정보)
    SealSpecialResult = 10,     // 특수 결과 봉인 -> 치명으로 흡수
    NegateMonsterMods = 11,     // 개체 보정/구간 변형 무효
    ReactionUpgradePlusLoad = 12, // 옵션 반응 +1단계, 부하 +magnitude
    ConvertLoadToReaction = 13, // 부하 절반 폐기, 반응 +magnitude
    MaidDepthReroll = 14,       // 전용: 해당 메이드 재굴림
    MaidPredictMinus = 15,      // 전용: 구간 공개 + 최종 +magnitude(음수)
    RemoveWorstAction = 16,     // 전용: 결과표 행동 1 제거(치명->위험 흡수)
    MaidRecoveryShift = 17,     // 전용: 회수 상한 +magnitude (패시브)
    AutoCatchRecovery = 18,     // 전용: 첫 회수 자동 포착 (패시브)
    StopAt199 = 19,             // 전용: 200 도달 시 1회 199 정지
}

/// <summary>능력 사용 제한. (§11)</summary>
public enum AbilityUseLimit { Passive = 0, PerDay = 1, PerService = 2, PerCampaign = 3 }

/// <summary>능력 해금의 지식 조건 종류. (§11)</summary>
public enum KnowledgeGateKind
{
    None = 0,
    AnyPartialCount = 1,        // 일부 파악 n개체
    AnyCompleteCount = 2,       // 완전 파악 n개체
    DepthWitnessCount = 3,      // 심층 목격 n회
    TypeServiceCount = 4,       // 해당 타입 접객 n회
    TypeCompleteCount = 5,      // 해당 타입 완전 파악 n
    TypeWitnessCount = 6,       // 해당 타입 심층 목격 n
}

/// <summary>엔딩 종류. (§15)</summary>
public enum EndingKindV3
{
    None = 0,
    FullHouseMorning = 1,   // S
    NormalBusiness = 2,     // A
    ClosingTime = 3,        // B
    Bankruptcy = 4,         // 폐업
    EmptyInn = 5,           // 전멸
}

/// <summary>캠페인의 세이브 가능 국면. (§14)</summary>
public enum CampaignPhaseV3
{
    SlotBoundary = 0,   // 접객 사이 - 저장 가능
    InService = 1,      // 접객/심층 중 - 저장 불가
    NightStart = 2,     // 밤 시작 - 저장 가능
    InNight = 3,        // 밤 처리 중 - 저장 불가
    DayEnd = 4,         // 일 종료 자동 저장 지점
    Finished = 5,
}

public static class GuesthouseV3EnumUtil
{
    public static ResearchType ToResearchType(this MonsterSpecies species) => species switch
    {
        MonsterSpecies.ParasiticEquipment => ResearchType.Physical,
        MonsterSpecies.PredatoryBinder => ResearchType.Physical,
        MonsterSpecies.MemoryDevourer => ResearchType.Mental,
        MonsterSpecies.ResonanceAmplifier => ResearchType.Empathic,
        _ => ResearchType.Physical,
    };

    public static int ToScore(this OptionIntensity intensity) => intensity switch
    {
        OptionIntensity.Heavy => 3,
        OptionIntensity.Medium => 1,
        _ => 0,
    };
}
