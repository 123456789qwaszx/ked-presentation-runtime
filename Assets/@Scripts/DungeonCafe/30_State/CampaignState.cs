using System;
using System.Collections.Generic;

/// <summary>
/// 캠페인 전체 상태. 세이브 대상의 루트가 된다.
/// </summary>
public sealed class CampaignState
{
    private readonly Dictionary<string, MaidRuntimeState> _maidById =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<MaidRuntimeState> _maids = new();
    private readonly List<DayCycleState> _completedDays = new();
    private readonly HashSet<MonsterSpecies> _encounteredSpecies = new();

    public ProgressionTuning Tuning { get; }

    public DayCycleState CurrentDay { get; private set; }

    public int TotalEnergy { get; private set; }

    public IReadOnlyList<MaidRuntimeState> Maids => _maids;
    public IReadOnlyList<DayCycleState> CompletedDays => _completedDays;
    public IReadOnlyCollection<MonsterSpecies> EncounteredSpecies => _encounteredSpecies;

    public CampaignState(ProgressionTuning tuning, IReadOnlyList<MaidProfile> maidProfiles)
    {
        Tuning = tuning;

        for (int i = 0; i < maidProfiles.Count; i++)
        {
            MaidRuntimeState maid = new(maidProfiles[i]);

            _maids.Add(maid);
            _maidById[maid.MaidId] = maid;
        }
    }

    public bool IsFinished => _completedDays.Count >= Tuning.CampaignDayCount;

    public int NextDayNumber => _completedDays.Count + 1;

    public bool TryFindMaid(string maidId, out MaidRuntimeState maid)
        => _maidById.TryGetValue(maidId ?? string.Empty, out maid);

    public DayCycleState BeginDay()
    {
        CurrentDay = new DayCycleState(NextDayNumber);
        return CurrentDay;
    }

    public void CompleteDay()
    {
        if (CurrentDay == null)
            return;

        TotalEnergy += CurrentDay.EnergyEarned;

        _completedDays.Add(CurrentDay);
        CurrentDay = null;
    }

    public void MarkSpeciesEncountered(MonsterSpecies species)
    {
        if (species == MonsterSpecies.None)
            return;

        _encounteredSpecies.Add(species);
    }

    public int CountLostMaids()
    {
        int count = 0;

        for (int i = 0; i < _maids.Count; i++)
        {
            if (_maids[i].IsLost)
                count++;
        }

        return count;
    }

    public int CountTotalIncidents()
    {
        int count = 0;

        for (int i = 0; i < _maids.Count; i++)
            count += _maids[i].IncidentCount;

        return count;
    }

    public int CountTotalMasteryLevels()
    {
        int total = 0;

        for (int i = 0; i < _maids.Count; i++)
        {
            IReadOnlyList<MaidMasteryTrack> tracks = _maids[i].MasteryTracks;

            for (int t = 0; t < tracks.Count; t++)
                total += tracks[t].Level;
        }

        return total;
    }

    public IReadOnlyList<MaidRuntimeState> CollectAssignableMaids(List<MaidRuntimeState> buffer)
    {
        buffer.Clear();

        for (int i = 0; i < _maids.Count; i++)
        {
            if (!_maids[i].CanBeAssigned)
                continue;

            buffer.Add(_maids[i]);
        }

        return buffer;
    }
}
