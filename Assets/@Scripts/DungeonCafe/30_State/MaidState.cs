using System;
using System.Collections.Generic;

/// <summary>축별 숙련 : 임계 120/300/550, 밤 커밋만. </summary>
public sealed class MasteryTrack
{
    public BurdenAxis Axis { get; }
    public int Level { get; private set; }
    public int Experience { get; private set; }

    public MasteryTrack(BurdenAxis axis) { Axis = axis; }

    public int NextThreshold(DungeonCafeTuning tuning)
        => Level >= tuning.MasteryThresholds.Count ? int.MaxValue : tuning.MasteryThresholds[Level];

    public bool IsEventReady(DungeonCafeTuning tuning)
        => Level < tuning.MasteryThresholds.Count && Experience >= NextThreshold(tuning);

    public void AddExperience(int amount) { if (amount > 0) Experience += amount; }

    public bool CommitLevelUp(DungeonCafeTuning tuning)
    {
        if (!IsEventReady(tuning)) return false;
        Level++;
        return true;
    }

    public void Restore(int level, int experience) { Level = Math.Max(0, level); Experience = Math.Max(0, experience); }
}

/// <summary>후유증 보유 인스턴스.</summary>
public sealed class AftereffectInstance
{
    public AftereffectDefinition Definition { get; }
    public AftereffectInstance(AftereffectDefinition definition) { Definition = definition; }
}

/// <summary> 동적 메이드 상태 </summary>
public sealed class MaidState
{
    private readonly MasteryTrack[] _mastery = new MasteryTrack[BurdenAxes.Count];
    private readonly List<AftereffectInstance> _aftereffects = new();
    private readonly List<string> _quirkIds = new();

    public MaidProfile Profile { get; }
    public MaidGaugeState Gauge { get; }

    public string MaidId => Profile.MaidId;
    public string DisplayName => Profile.DisplayName;
    public AxisTriple Aptitude => Profile.Aptitude;

    public bool HasRescueTicket { get; private set; } = true;   // 메이드당 1
    public int TotalCollapseCount { get; private set; }
    public bool IsLost { get; private set; }

    public int RelationPoints { get; private set; }
    public int TrustCount { get; private set; }
    public int DependCount { get; private set; }

    /// <summary>심층 탈출 등 관계 노선 비가역 플래그. </summary>
    public bool HasDepthScar { get; private set; }

    public IReadOnlyList<AftereffectInstance> Aftereffects => _aftereffects;
    public IReadOnlyList<string> QuirkIds => _quirkIds;
    public const int QuirkSlotCount = 3;

    public MaidState(MaidProfile profile, DungeonCafeTuning tuning)
    {
        Profile = profile;
        Gauge = new MaidGaugeState(tuning.TotalCollapseThreshold);
        for (int i = 0; i < BurdenAxes.Count; i++)
            _mastery[i] = new MasteryTrack(BurdenAxes.FromIndex(i));
    }

    public MasteryTrack GetMastery(BurdenAxis axis) => _mastery[(int)axis];
    
    public bool IsPresent(int dayNumber) => !IsLost && dayNumber >= Profile.UnlockDay;

    public bool HasAftereffect => _aftereffects.Count > 0;
    public bool HasQuirk => _quirkIds.Count > 0;
    public bool HasQuirkId(string id) => _quirkIds.Contains(id);

    public AftereffectInstance FindAftereffect(string id)
    {
        for (int i = 0; i < _aftereffects.Count; i++)
            if (_aftereffects[i].Definition.Id == id) return _aftereffects[i];
        return null;
    }

    public void AddAftereffect(AftereffectDefinition def)
    {
        if (def == null || FindAftereffect(def.Id) != null) return;
        _aftereffects.Add(new AftereffectInstance(def));
    }

    public void RemoveAftereffect(AftereffectInstance instance) => _aftereffects.Remove(instance);

    /// <summary>기벽 추가. 사고성이 만석에 들어오면 안정 기벽 1개(evictId)를 밀어낸다. </summary>
    public bool AddQuirk(string quirkId, bool isAccident, string evictStableId = null)
    {
        if (string.IsNullOrEmpty(quirkId) || _quirkIds.Contains(quirkId)) return false;

        if (_quirkIds.Count >= QuirkSlotCount)
        {
            if (!isAccident) return false;
            if (evictStableId != null && _quirkIds.Remove(evictStableId)) { /* 지정 축출 */ }
            else if (_quirkIds.Count >= QuirkSlotCount) _quirkIds.RemoveAt(0);
        }

        _quirkIds.Add(quirkId);
        return true;
    }

    public void AddRelation(int points, RelationDirection direction)
    {
        if (points > 0)
        {
            RelationPoints += points;
            if (direction == RelationDirection.Trust) TrustCount++; else DependCount++;
        }
        else if (points < 0)
        {
            RelationPoints = Math.Max(0, RelationPoints + points);
        }
    }

    public RelationDirection DominantDirection
        => DependCount > TrustCount
            ? RelationDirection.Depend 
            : RelationDirection.Trust;

    public void MarkDepthScar() => HasDepthScar = true;

    /// <summary>완전 붕괴 처리 결과 반영. </summary>
    public void MarkTotalCollapse(bool rescued)
    {
        TotalCollapseCount++;
        if (rescued) HasRescueTicket = false;
        else IsLost = true;
    }

    public void RestoreCore(
        bool hasTicket, int totalCollapses, bool isLost,
        int relationPoints, int trust, int depend, bool depthScar)
    {
        HasRescueTicket = hasTicket; TotalCollapseCount = totalCollapses; IsLost = isLost;
        RelationPoints = relationPoints; TrustCount = trust; DependCount = depend; HasDepthScar = depthScar;
    }

    public void RestoreQuirks(IReadOnlyList<string> ids)
    {
        _quirkIds.Clear();
        if (ids != null) _quirkIds.AddRange(ids);
    }
}