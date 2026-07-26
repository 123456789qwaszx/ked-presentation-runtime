using System.Collections.Generic;

/// <summary>
/// 캠페인 진행 중 변하는 메이드 상태.
/// 정의(MaidProfile)는 참조만 하고 복제하지 않는다.
/// </summary>
public sealed class MaidRuntimeState
{
    private readonly MaidMasteryTrack[] _masteryTracks = new MaidMasteryTrack[BurdenAxes.Count];

    public MaidProfile Profile { get; }
    public MaidBurdenState Burden { get; }

    public string MaidId => Profile.MaidId;
    public string DisplayName => Profile.DisplayName;
    public AxisTriple Aptitude => Profile.Aptitude;

    /// <summary>통제 상실까지 간 접객 횟수.</summary>
    public int IncidentCount { get; private set; }

    /// <summary>통제 상실 이후 회수되지 못한 상태. 이 경우 이후 배정이 막힌다.</summary>
    public bool IsLost { get; private set; }

    public MaidRuntimeState(MaidProfile profile)
    {
        Profile = profile;
        Burden = new MaidBurdenState(profile.CollapseLimit);

        for (int i = 0; i < BurdenAxes.Count; i++)
            _masteryTracks[i] = new MaidMasteryTrack(BurdenAxes.FromIndex(i));
    }

    public MaidMasteryTrack GetMastery(BurdenAxis axis) => _masteryTracks[(int)axis];

    public IReadOnlyList<MaidMasteryTrack> MasteryTracks => _masteryTracks;

    /// <summary>
    /// 배정 가능 여부. 하루 안에 같은 메이드를 다시 투입하는 것은 허용하되,
    /// 통제 상실 후 회수되지 못한 메이드는 배정 후보에서 제외한다.
    /// </summary>
    public bool CanBeAssigned => !IsLost;

    public void MarkIncident(bool recovered)
    {
        IncidentCount++;

        if (!recovered)
            IsLost = true;
    }

    public bool TryFindReadyMasteryAxis(ProgressionTuning tuning, out BurdenAxis axis)
    {
        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            if (!_masteryTracks[i].IsEventReady(tuning))
                continue;

            axis = BurdenAxes.FromIndex(i);
            return true;
        }

        axis = BurdenAxis.Physical;
        return false;
    }

    public override string ToString()
        => $"{DisplayName} 대응력{Aptitude} 부담{Burden.Snapshot()}";
}
