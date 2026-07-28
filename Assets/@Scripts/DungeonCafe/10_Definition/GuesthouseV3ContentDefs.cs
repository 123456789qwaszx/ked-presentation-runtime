using System;
using System.Collections.Generic;

// v3 콘텐츠 정의 레코드 모음. 전부 불변, Unity 무의존.
// 저작은 GuesthouseV3Content(코드 내장)가 기본이고, SO 이관 시 각 레코드 1:1 매핑한다.

/// <summary>메이드 정의. (§12.1)</summary>
public sealed class MaidProfileV3
{
    public string MaidId { get; }
    public string DisplayName { get; }
    public int UnlockDay { get; }
    public AxisTriple Aptitude { get; }
    public string ProposalStyleKey { get; }

    /// <summary>심층 회수 상한 변형. (시온 +8)</summary>
    public int TraitRecoveryShift { get; }
    /// <summary>심층 위험 상한 변형. (루이 -3 = 치명 하한 58)</summary>
    public int TraitRiskyShift { get; }
    /// <summary>상태이상 주사위 보정 배율 %. (아리에 50 = 절반)</summary>
    public int StatusModifierPercent { get; }

    /// <summary>방치 자율행동: 발동 확률 %.</summary>
    public int DispositionChancePercent { get; }
    public string DispositionKey { get; }

    public MaidProfileV3(
        string maidId, string displayName, int unlockDay, AxisTriple aptitude,
        string proposalStyleKey, int traitRecoveryShift, int traitRiskyShift,
        int statusModifierPercent, int dispositionChancePercent, string dispositionKey)
    {
        MaidId = maidId; DisplayName = displayName; UnlockDay = unlockDay;
        Aptitude = aptitude; ProposalStyleKey = proposalStyleKey;
        TraitRecoveryShift = traitRecoveryShift; TraitRiskyShift = traitRiskyShift;
        StatusModifierPercent = statusModifierPercent;
        DispositionChancePercent = dispositionChancePercent; DispositionKey = dispositionKey;
    }
}

/// <summary>낮 행동 옵션. (§2.2)</summary>
public sealed class ServiceOptionV3
{
    public OptionIntensity Intensity { get; }
    public BurdenAxis LoadAxis { get; }
    public LoadRange Range { get; }
    public string NodeKey { get; }
    /// <summary>무기/감응 등 기벽 태그 (MediumReactionUpgrade 매칭용).</summary>
    public string Tag { get; }

    public ServiceOptionV3(OptionIntensity intensity, BurdenAxis loadAxis, string nodeKey, string tag = null)
    {
        Intensity = intensity;
        LoadAxis = loadAxis;
        Range = intensity switch
        {
            OptionIntensity.Heavy => LoadRange.Heavy,
            OptionIntensity.Medium => LoadRange.Medium,
            _ => LoadRange.Light,
        };
        NodeKey = nodeKey;
        Tag = tag ?? string.Empty;
    }
}

/// <summary>접객 비트: 상황 노드 + 옵션 3. (§2.1)</summary>
public sealed class ServiceBeatV3
{
    public string SituationNodeKey { get; }
    public IReadOnlyList<ServiceOptionV3> Options { get; }

    public ServiceBeatV3(string situationNodeKey, IReadOnlyList<ServiceOptionV3> options)
    {
        SituationNodeKey = situationNodeKey;
        Options = options;
    }
}

/// <summary>심층 결과표: 4구간 + 개체 고유 특수 1. (§13.3)</summary>
public sealed class DepthActionSet
{
    public string RiskyNodeKey { get; }
    public string FatalNodeKey { get; }
    public string SpecialNodeKey { get; }
    public DepthActionSet(string risky, string fatal, string special)
    {
        RiskyNodeKey = risky; FatalNodeKey = fatal; SpecialNodeKey = special;
    }
}

/// <summary>몬스터 개체 정의. (§13.2)</summary>
public sealed class MonsterProfileV3
{
    public string MonsterId { get; }
    public string DisplayName { get; }
    public MonsterSpecies Species { get; }
    public int AppearDay { get; }
    public BurdenAxis DemandAxis { get; }
    public int RequiredSatisfaction { get; }
    /// <summary>부하 보정. 갑주무리는 강 옵션 한정(HeavyOnly).</summary>
    public int LoadModifier { get; }
    public bool LoadModifierHeavyOnly { get; }
    public MonsterSpecialRule SpecialRule { get; }
    public IReadOnlyList<ServiceBeatV3> Beats { get; }
    public DepthActionSet DepthActions { get; }
    public string ReservationPostText { get; }
    public string PhoneCallNodeName { get; }

    public MonsterProfileV3(
        string monsterId, string displayName, MonsterSpecies species, int appearDay,
        BurdenAxis demandAxis, int requiredSatisfaction,
        int loadModifier, bool loadModifierHeavyOnly, MonsterSpecialRule specialRule,
        IReadOnlyList<ServiceBeatV3> beats, DepthActionSet depthActions,
        string reservationPostText, string phoneCallNodeName)
    {
        MonsterId = monsterId; DisplayName = displayName; Species = species; AppearDay = appearDay;
        DemandAxis = demandAxis; RequiredSatisfaction = requiredSatisfaction;
        LoadModifier = loadModifier; LoadModifierHeavyOnly = loadModifierHeavyOnly;
        SpecialRule = specialRule; Beats = beats; DepthActions = depthActions;
        ReservationPostText = reservationPostText; PhoneCallNodeName = phoneCallNodeName;
    }
}

/// <summary>종족 규약: 파국 노드. 탈출 경로는 전 종족 공통 = 회수 구간뿐. (§13.1)</summary>
public sealed class SpeciesProtocolV3
{
    public MonsterSpecies Species { get; }
    public string DisplayName { get; }
    public string ControlLossNodeName { get; }
    public string CollapseEndingNodeName { get; }
    public SpeciesProtocolV3(MonsterSpecies species, string displayName, string controlLossNode, string endingNode)
    {
        Species = species; DisplayName = displayName;
        ControlLossNodeName = controlLossNode; CollapseEndingNodeName = endingNode;
    }
}

/// <summary>후유증 정의. tremor·brand. 붕괴 0(tremor) 또는 관리붕괴(brand)로 해소. (§9)</summary>
public sealed class AftereffectDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    /// <summary>보유 중 낮 부하 판정에 더해지는 페널티. (떨림 +2)</summary>
    public int DayLoadPenalty { get; }
    /// <summary>보유 중 태그 종족 심층 주사위 보정. (각인 +7)</summary>
    public int DepthDieModifier { get; }
    public MonsterSpecies TaggedSpecies { get; }

    public AftereffectDefinition(
        string id, string displayName, int dayLoadPenalty,
        int depthDieModifier, MonsterSpecies taggedSpecies)
    {
        Id = id; DisplayName = displayName; DayLoadPenalty = dayLoadPenalty;
        DepthDieModifier = depthDieModifier; TaggedSpecies = taggedSpecies;
    }
}
/// <summary>기벽 정의. 계약 4필드: 효과 / 판정 보정 / 대사 세트 / 밤 이벤트. (§10)</summary>
public sealed class QuirkDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    /// <summary>null 이면 공용(사고성).</summary>
    public string OwnerMaidId { get; }
    public bool IsAccident { get; }
    public QuirkEffectKind EffectKind { get; }
    public int Magnitude { get; }
    public int SecondaryMagnitude { get; }
    public MonsterSpecies TaggedSpecies { get; }
    public BurdenAxis TaggedAxis { get; }
    public string OptionTag { get; }
    public string DialogueSetKey { get; }
    public string NightEventKey { get; }

    public QuirkDefinition(
        string id, string displayName, string ownerMaidId, bool isAccident,
        QuirkEffectKind effectKind, int magnitude, int secondaryMagnitude,
        MonsterSpecies taggedSpecies, BurdenAxis taggedAxis, string optionTag,
        string dialogueSetKey, string nightEventKey)
    {
        Id = id; DisplayName = displayName; OwnerMaidId = ownerMaidId; IsAccident = isAccident;
        EffectKind = effectKind; Magnitude = magnitude; SecondaryMagnitude = secondaryMagnitude;
        TaggedSpecies = taggedSpecies; TaggedAxis = taggedAxis; OptionTag = optionTag ?? string.Empty;
        DialogueSetKey = dialogueSetKey; NightEventKey = nightEventKey;
    }
}

/// <summary>능력 해금 4-튜플 + 사용 제한. (§11)</summary>
public sealed class PlayerAbilityDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public AbilityEffectKind EffectKind { get; }
    public int Magnitude { get; }
    public AbilityUseLimit UseLimit { get; }
    public int UseCount { get; }
    public int ShopLevelRequired { get; }
    public int DesireCost { get; }
    public KnowledgeGateKind KnowledgeGate { get; }
    public int KnowledgeCount { get; }
    public ResearchType KnowledgeType { get; }
    /// <summary>전용 능력: 소유 메이드 + 관계 단계. null 이면 공용.</summary>
    public string OwnerMaidId { get; }
    public int RelationStageRequired { get; }
    /// <summary>전용 능력은 장착 슬롯을 차지하지 않는다.</summary>
    public bool OccupiesSlot => OwnerMaidId == null;

    public PlayerAbilityDefinition(
        string id, string displayName, AbilityEffectKind effectKind, int magnitude,
        AbilityUseLimit useLimit, int useCount, int shopLevelRequired, int desireCost,
        KnowledgeGateKind knowledgeGate, int knowledgeCount, ResearchType knowledgeType,
        string ownerMaidId = null, int relationStageRequired = 0)
    {
        Id = id; DisplayName = displayName; EffectKind = effectKind; Magnitude = magnitude;
        UseLimit = useLimit; UseCount = useCount; ShopLevelRequired = shopLevelRequired;
        DesireCost = desireCost; KnowledgeGate = knowledgeGate; KnowledgeCount = knowledgeCount;
        KnowledgeType = knowledgeType; OwnerMaidId = ownerMaidId;
        RelationStageRequired = relationStageRequired;
    }
}

/// <summary>캘린더 1일분. (§1)</summary>
public sealed class CampaignDayPlan
{
    public int DayNumber { get; }
    public int ServiceSlots { get; }
    public int Quota { get; }
    public CampaignDayPlan(int dayNumber, int serviceSlots, int quota)
    {
        DayNumber = dayNumber;
        ServiceSlots = serviceSlots;
        Quota = quota;
    }
}

/// <summary>v3 콘텐츠 루트. 조회는 전부 여기로.</summary>
public sealed class GuesthouseV3ContentDB
{
    private readonly Dictionary<string, MaidProfileV3> _maids = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MonsterProfileV3> _monsters = new(StringComparer.Ordinal);
    private readonly Dictionary<MonsterSpecies, SpeciesProtocolV3> _protocols = new();
    private readonly Dictionary<string, AftereffectDefinition> _aftereffects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, QuirkDefinition> _quirks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PlayerAbilityDefinition> _abilities = new(StringComparer.Ordinal);

    public IReadOnlyList<MaidProfileV3> Maids { get; }
    public IReadOnlyList<MonsterProfileV3> Monsters { get; }
    public IReadOnlyList<QuirkDefinition> Quirks { get; }
    public IReadOnlyList<PlayerAbilityDefinition> Abilities { get; }
    public IReadOnlyList<CampaignDayPlan> Calendar { get; }

    public GuesthouseV3ContentDB(
        IReadOnlyList<MaidProfileV3> maids,
        IReadOnlyList<MonsterProfileV3> monsters,
        IReadOnlyList<SpeciesProtocolV3> protocols,
        IReadOnlyList<AftereffectDefinition> aftereffects,
        IReadOnlyList<QuirkDefinition> quirks,
        IReadOnlyList<PlayerAbilityDefinition> abilities,
        IReadOnlyList<CampaignDayPlan> calendar)
    {
        Maids = maids; 
        foreach (MaidProfileV3 m in maids)
            _maids[m.MaidId] = m;
        
        Monsters = monsters; 
        foreach (MonsterProfileV3 m in monsters)
            _monsters[m.MonsterId] = m;
        
        foreach (SpeciesProtocolV3 p in protocols)
            _protocols[p.Species] = p;
        
        foreach (AftereffectDefinition a in aftereffects)
            _aftereffects[a.Id] = a;
        
        Quirks = quirks; 
        foreach (QuirkDefinition q in quirks)
            _quirks[q.Id] = q;
        
        Abilities = abilities;
        foreach (PlayerAbilityDefinition a in abilities)
            _abilities[a.Id] = a;
        
        Calendar = calendar;
    }

    public MaidProfileV3 GetMaid(string id) => _maids.TryGetValue(id, out var v) ? v : null;
    public MonsterProfileV3 GetMonster(string id) => _monsters.TryGetValue(id, out var v) ? v : null;
    public SpeciesProtocolV3 GetProtocol(MonsterSpecies s) => _protocols.TryGetValue(s, out var v) ? v : null;
    public AftereffectDefinition GetAftereffect(string id) => _aftereffects.TryGetValue(id, out var v) ? v : null;
    public QuirkDefinition GetQuirk(string id) => _quirks.TryGetValue(id, out var v) ? v : null;
    public PlayerAbilityDefinition GetAbility(string id) => _abilities.TryGetValue(id, out var v) ? v : null;

    public CampaignDayPlan GetDayPlan(int dayNumber)
    {
        for (int i = 0; i < Calendar.Count; i++)
            if (Calendar[i].DayNumber == dayNumber)
                return Calendar[i];
        
        return null;
    }

    public int CampaignDayCount => Calendar.Count;

    /// <summary>해당 일차에 등장 가능한 개체 풀 (AppearDay ≤ day).</summary>
    public List<MonsterProfileV3> GetMonsterPool(int dayNumber)
    {
        var pool = new List<MonsterProfileV3>();
        for (int i = 0; i < Monsters.Count; i++)
            if (Monsters[i].AppearDay <= dayNumber) pool.Add(Monsters[i]);
        return pool;
    }
}
