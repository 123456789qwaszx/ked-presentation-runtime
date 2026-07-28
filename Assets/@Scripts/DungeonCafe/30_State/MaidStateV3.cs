using System;
using System.Collections.Generic;

/// <summary>축별 숙련 v3: 임계 120/300/550, 밤 커밋만. (§12.3)</summary>
public sealed class MasteryTrackV3
{
    public BurdenAxis Axis { get; }
    public int Level { get; private set; }
    public int Experience { get; private set; }

    public MasteryTrackV3(BurdenAxis axis) { Axis = axis; }

    public int NextThreshold(GuesthouseTuningV3 tuning)
        => Level >= tuning.MasteryThresholds.Count ? int.MaxValue : tuning.MasteryThresholds[Level];

    public bool IsEventReady(GuesthouseTuningV3 tuning)
        => Level < tuning.MasteryThresholds.Count && Experience >= NextThreshold(tuning);

    public void AddExperience(int amount) { if (amount > 0) Experience += amount; }

    public bool CommitLevelUp(GuesthouseTuningV3 tuning)
    {
        if (!IsEventReady(tuning)) return false;
        Level++;
        return true;
    }

    public void Restore(int level, int experience) { Level = Math.Max(0, level); Experience = Math.Max(0, experience); }
}

/// <summary>후유증 보유 인스턴스. (§9)</summary>
public sealed class AftereffectInstance
{
    public AftereffectDefinition Definition { get; }
    public int CaresApplied { get; private set; }
    public int NeglectDaysPassed { get; private set; }
    public int DaysHeld { get; private set; }
    public int BlockDaysLeft { get; private set; }

    public AftereffectInstance(AftereffectDefinition definition)
    {
        Definition = definition;
        BlockDaysLeft = definition.BlocksAssignment
            ? (definition.BlockDays > 0 ? definition.BlockDays : int.MaxValue)
            : 0;
    }

    public bool BlocksAssignmentNow => BlockDaysLeft > 0;

    /// <summary>안정 1회 적용. 해소되면 true.</summary>
    public bool ApplyCare()
    {
        CaresApplied++;
        return CaresApplied >= Definition.CareCuresNeeded;
    }

    /// <summary>방치 하루 경과. 해소되면 true, 영구화 도달이면 permanentize 로 알린다.</summary>
    public bool AdvanceNight(GuesthouseTuningV3 tuning, out bool permanentize)
    {
        NeglectDaysPassed++;
        DaysHeld++;
        if (BlockDaysLeft > 0 && BlockDaysLeft != int.MaxValue) BlockDaysLeft--;

        permanentize = !string.IsNullOrEmpty(Definition.PermanentizeQuirkId)
                       && DaysHeld >= tuning.BrandPermanentizeDays;

        return Definition.NeglectHealDays > 0 && NeglectDaysPassed >= Definition.NeglectHealDays;
    }

    public void Restore(int cares, int neglectDays, int daysHeld, int blockLeft)
    {
        CaresApplied = cares; NeglectDaysPassed = neglectDays; DaysHeld = daysHeld; BlockDaysLeft = blockLeft;
    }
}

/// <summary>캠페인 중 변하는 메이드 상태 v3 통합. (§12)</summary>
public sealed class MaidStateV3
{
    private readonly MasteryTrackV3[] _mastery = new MasteryTrackV3[BurdenAxes.Count];
    private readonly List<AftereffectInstance> _aftereffects = new();
    private readonly List<string> _quirkIds = new();

    public MaidProfileV3 Profile { get; }
    public MaidGaugeState Gauge { get; }

    public string MaidId => Profile.MaidId;
    public string DisplayName => Profile.DisplayName;
    public AxisTriple Aptitude => Profile.Aptitude;

    public bool HasRescueTicket { get; private set; } = true;   // §5: 메이드당 1
    public int TotalCollapseCount { get; private set; }
    public bool IsLost { get; private set; }

    public int RelationPoints { get; private set; }
    public int TrustCount { get; private set; }
    public int DependCount { get; private set; }

    /// <summary>심층 탈출 등 관계 노선 비가역 플래그. (§3.4)</summary>
    public bool HasDepthScar { get; private set; }

    public IReadOnlyList<AftereffectInstance> Aftereffects => _aftereffects;
    public IReadOnlyList<string> QuirkIds => _quirkIds;
    public const int QuirkSlotCount = 3;                        // §10

    public MaidStateV3(MaidProfileV3 profile, GuesthouseTuningV3 tuning)
    {
        Profile = profile;
        Gauge = new MaidGaugeState(tuning.TotalCollapseThreshold);
        for (int i = 0; i < BurdenAxes.Count; i++)
            _mastery[i] = new MasteryTrackV3(BurdenAxes.FromIndex(i));
    }

    public MasteryTrackV3 GetMastery(BurdenAxis axis) => _mastery[(int)axis];

    public bool CanBeAssigned(int dayNumber)
    {
        if (IsLost || dayNumber < Profile.UnlockDay) 
            return false;
        
        for (int i = 0; i < _aftereffects.Count; i++)
            if (_aftereffects[i].BlocksAssignmentNow)
                return false;
        
        return true;
    }

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

    /// <summary>기벽 추가. 사고성이 만석에 들어오면 안정 기벽 1개(evictId)를 밀어낸다. (§10)</summary>
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

    public bool RemoveStableQuirk(string quirkId) => _quirkIds.Remove(quirkId);

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
        => DependCount > TrustCount ? RelationDirection.Depend : RelationDirection.Trust;

    public void MarkDepthScar() => HasDepthScar = true;

    /// <summary>완전 붕괴 처리 결과 반영. (§5)</summary>
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
