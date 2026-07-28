using System;
using System.Collections.Generic;

/// <summary>개체별 이해도. </summary>
public sealed class UnderstandingState
{
    private readonly Dictionary<string, int> _points = new(StringComparer.Ordinal);
    private readonly HashSet<string> _phoneCalled = new(StringComparer.Ordinal);
    private readonly HashSet<string> _depthWitnessed = new(StringComparer.Ordinal); // "monsterId" 단위 (첫 진입)
    private readonly HashSet<string> _oneTimeFlags = new(StringComparer.Ordinal);   // "flag:monsterId:maidId"

    public int GetPoints(string monsterId)
        => _points.TryGetValue(monsterId, out int p) ? p : 0;

    public void AddPoints(string monsterId, int amount)
    {
        if (amount <= 0) return;
        _points[monsterId] = GetPoints(monsterId) + amount;
    }

    public UnderstandingTier GetTier(string monsterId, DungeonCafeTuning tuning)
    {
        int p = GetPoints(monsterId);
        IReadOnlyList<int> t = tuning.UnderstandingTierThresholds;
        if (p >= t[2]) return UnderstandingTier.Complete;
        if (p >= t[1]) return UnderstandingTier.Advanced;
        if (p >= t[0]) return UnderstandingTier.Partial;
        return UnderstandingTier.Unknown;
    }

    public bool MarkPhoneCalled(string monsterId) => _phoneCalled.Add(monsterId);
    public bool MarkDepthWitnessed(string monsterId) => _depthWitnessed.Add(monsterId);
    public int DepthWitnessTotal => _depthWitnessed.Count;

    /// <summary>1회성 플래그 (심층 페이지/회상/사고 기벽 - 개체x메이드). 신규 등록 시 true. </summary>
    public bool TryClaimOneTime(string flag, string monsterId, string maidId)
        => _oneTimeFlags.Add($"{flag}:{monsterId}:{maidId}");

    // 집계 (능력 게이트용)
    public int CountAtTier(DungeonCafeContentDB content, DungeonCafeTuning tuning, UnderstandingTier atLeast)
    {
        int count = 0;
        
        for (int i = 0; i < content.Monsters.Count; i++)
            if (GetTier(content.Monsters[i].MonsterId, tuning) >= atLeast)
                count++;
        
        return count;
    }

    public int CountTypeAtTier(DungeonCafeContentDB content, DungeonCafeTuning tuning, ResearchType type, UnderstandingTier atLeast)
    {
        int count = 0;
        for (int i = 0; i < content.Monsters.Count; i++)
        {
            MonsterProfile m = content.Monsters[i];
            if (m.Species.ToResearchType() == type && GetTier(m.MonsterId, tuning) >= atLeast) count++;
        }
        return count;
    }
    
    // 주어진 개체 중 이해도가 가장 낮은 하나.
    public string FindLeastUnderstood(IReadOnlyList<MonsterProfile> monsters)
    {
        string selectedId = null;
        int selectedPoints = int.MaxValue;

        for (int i = 0; i < monsters.Count; i++)
        {
            string monsterId = monsters[i].MonsterId;
            int points = GetPoints(monsterId);

            if (points < selectedPoints)
            {
                selectedId = monsterId;
                selectedPoints = points;
            }
        }

        return selectedId;
    }

    public IEnumerable<KeyValuePair<string, int>> AllPoints => _points;
    public IEnumerable<string> PhoneCalled => _phoneCalled;
    public IEnumerable<string> DepthWitnessed => _depthWitnessed;
    public IEnumerable<string> OneTimeFlags => _oneTimeFlags;
    public void RestorePoint(string id, int p) => _points[id] = p;
    public void RestorePhone(string id) => _phoneCalled.Add(id);
    public void RestoreWitness(string id) => _depthWitnessed.Add(id);
    public void RestoreFlag(string f) => _oneTimeFlags.Add(f);
}

/// <summary>플레이어 능력 보유/장착/사용 횟수. </summary>
public sealed class PlayerAbilityState
{
    private readonly HashSet<string> _owned = new(StringComparer.Ordinal);
    private readonly List<string> _equipped = new();
    private readonly Dictionary<string, int> _usedToday = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _usedThisService = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _usedThisCampaign = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Owned => _owned;
    public IReadOnlyList<string> Equipped => _equipped;

    public bool Owns(string id) => _owned.Contains(id);
    public void Grant(string id) { if (!string.IsNullOrEmpty(id)) _owned.Add(id); }

    public bool Equip(string id, int slotLimit)
    {
        if (!_owned.Contains(id) || _equipped.Contains(id)) return false;
        if (_equipped.Count >= slotLimit) return false;
        _equipped.Add(id);
        return true;
    }

    public bool Unequip(string id) => _equipped.Remove(id);

    /// <summary>전용 능력은 슬롯 무관 상시 사용 가능. </summary>
    public bool IsAvailable(PlayerAbilityDefinition def)
        => _owned.Contains(def.Id) && (!def.OccupiesSlot || _equipped.Contains(def.Id));

    public int GetUsed(PlayerAbilityDefinition def) => def.UseLimit switch
    {
        AbilityUseLimit.PerDay => _usedToday.TryGetValue(def.Id, out int d) ? d : 0,
        AbilityUseLimit.PerService => _usedThisService.TryGetValue(def.Id, out int s) ? s : 0,
        AbilityUseLimit.PerCampaign => _usedThisCampaign.TryGetValue(def.Id, out int c) ? c : 0,
        _ => 0,
    };

    public bool CanUse(PlayerAbilityDefinition def)
        => IsAvailable(def) && (def.UseLimit == AbilityUseLimit.Passive || GetUsed(def) < def.UseCount);

    public void MarkUsed(PlayerAbilityDefinition def)
    {
        Dictionary<string, int> book = def.UseLimit switch
        {
            AbilityUseLimit.PerDay => _usedToday,
            AbilityUseLimit.PerService => _usedThisService,
            AbilityUseLimit.PerCampaign => _usedThisCampaign,
            _ => null,
        };
        if (book == null) return;
        book[def.Id] = (book.TryGetValue(def.Id, out int n) ? n : 0) + 1;
    }

    public void StartNewDay() => _usedToday.Clear();
    public void StartNewService() => _usedThisService.Clear();

    public IEnumerable<KeyValuePair<string, int>> CampaignUses => _usedThisCampaign;
    public void RestoreCampaignUse(string id, int n) => _usedThisCampaign[id] = n;
}

/// <summary>판정 커밋 로그. 커밋마다 롤백 버전이 오르고, 표현 계층이 이를 소비해 롤백을 자른다. </summary>
public sealed class JudgmentCommitLog
{
    public readonly struct Entry
    {
        public string Kind { get; }
        public ulong RngStateBefore { get; }
        public int Value { get; }
        public Entry(string kind, ulong rngStateBefore, int value)
        { Kind = kind; RngStateBefore = rngStateBefore; Value = value; }
    }

    private readonly List<Entry> _entries = new();

    /// <summary>표현 계층(VNLoadSeekDriver 등)의 버전 확인 용. 커밋마다 +1.</summary>
    public int RollbackBarrierVersion { get; private set; }

    public IReadOnlyList<Entry> Entries => _entries;

    public void Commit(string kind, ulong rngStateBefore, int value)
    {
        _entries.Add(new Entry(kind, rngStateBefore, value));
        RollbackBarrierVersion++;
    }

    public void RestoreBarrier(int version) => RollbackBarrierVersion = version;
}

/// <summary>하루 진행 상태.</summary>
public sealed class DayState
{
    public int DayNumber => Plan.DayNumber;
    public CampaignDayPlan Plan { get; }
    public IReadOnlyList<MonsterProfile> Bookings { get; }

    public int CompletedSlots { get; set; }

    public DayState(CampaignDayPlan plan, IReadOnlyList<MonsterProfile> bookings)
    {
        Plan = plan;

        var snapshot = new MonsterProfile[bookings.Count];

        for (int i = 0; i < bookings.Count; i++)
            snapshot[i] = bookings[i];
        
        Bookings = snapshot;
    }
}

/// <summary>캠페인 루트 상태. 세이브의 단일 진입점.</summary>
public sealed class CampaignState
{
    private readonly List<MaidState> _maids = new();

    public DungeonCafeContentDB Content { get; }
    public DungeonCafeTuning Tuning { get; }
    public DeterministicRng Rng { get; }
    public DesireLedger Ledger { get; } = new();
    public UnderstandingState Understanding { get; } = new();
    public PlayerAbilityState Abilities { get; } = new();
    public JudgmentCommitLog CommitLog { get; } = new();

    public int CurrentDayNumber { get; set; } = 1;
    public int BankruptcyCount { get; set; }
    public CampaignPhase Phase { get; set; } = CampaignPhase.SlotBoundary;
    public EndingKind Ending { get; set; } = EndingKind.None;
    public List<string> RuinedRouteMaidIds { get; } = new();

    /// <summary>다음 밤 선택지에 추가될 "먼저 요구하는 이벤트" 예약: (maidId, quirkId).</summary>
    public List<(string maidId, string quirkId)> PendingQuirkRequests { get; } = new();

    public IReadOnlyList<MaidState> Maids => _maids;

    public CampaignState(DungeonCafeContentDB content, DungeonCafeTuning tuning, ulong seed)
    {
        Content = content;
        Tuning = tuning;
        Rng = new DeterministicRng(seed);
        for (int i = 0; i < content.Maids.Count; i++)
            _maids.Add(new MaidState(content.Maids[i], tuning));
    }

    public MaidState GetMaid(string maidId)
    {
        for (int i = 0; i < _maids.Count; i++)
            if (_maids[i].MaidId == maidId)
                return _maids[i];
        
        return null;
    }
    
    public List<MaidState> GetPresent(int dayNumber)
    {
        var list = new List<MaidState>();
        
        for (int i = 0; i < _maids.Count; i++)
            if (_maids[i].IsPresent(dayNumber)) list.Add(_maids[i]);
        
        return list;
    }

    public int AliveMaidCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < _maids.Count; i++) if (!_maids[i].IsLost) n++;
            return n;
        }
    }

    public int ShopLevel => ShopLevelRule.Resolve(Ledger.Lifetime, Tuning);

    /// <summary>커밋 원자 연산: rng 상태 기록 -> 굴림 -> 로그 -> 발행 및 기록 절단. </summary>
    public int CommitRoll(string kind, int min, int max)
    {
        ulong before = Rng.State;
        int value = Rng.NextInclusive(min, max);
        CommitLog.Commit(kind, before, value);
        return value;
    }

    public bool CommitPercent(string kind, int chance)
    {
        ulong before = Rng.State;
        bool ok = Rng.RollPercent(chance);
        CommitLog.Commit(kind, before, ok ? 1 : 0);
        return ok;
    }
    
    public bool RegisterPhoneCall(string monsterId)
        => UnderstandingRule.GrantPhoneCall(this, monsterId);
    
    // 예약된 "먼저 요구하는 이벤트"를 꺼내고 비운다
    public List<(string maidId, string quirkId)> DrainPendingQuirkRequests()
    {
        var drained = new List<(string maidId, string quirkId)>(PendingQuirkRequests);
        PendingQuirkRequests.Clear();
        return drained;
    }

    public bool CanSaveNow => Phase is CampaignPhase.SlotBoundary
        or CampaignPhase.NightStart or CampaignPhase.DayEnd or CampaignPhase.Finished;
}