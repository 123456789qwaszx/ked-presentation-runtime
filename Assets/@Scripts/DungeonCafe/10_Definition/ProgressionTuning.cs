using System;
using System.Collections.Generic;

/// <summary>
/// 밸런스 상수 묶음.
/// 규칙 클래스는 상수를 직접 들고 있지 않고 전부 이 객체를 통해 읽는다.
/// ProgressionTuningSO 로 저작하거나 CreateDefault() 로 코드 기본값을 쓴다.
/// </summary>
public sealed class ProgressionTuning
{
    private readonly CollapseMultiplierBand[] _multiplierBands;
    private readonly int[][] _masteryThresholdsByAxis;

    // ---- 부담 누적 ----

    /// <summary>대응력 1당 감소하는 부하량.</summary>
    public int AptitudeMitigationPerPoint { get; }

    /// <summary>부하가 0보다 컸다면 최소한 이만큼은 누적된다. 완전 무효화를 막는다.</summary>
    public int MinimumAppliedLoad { get; }

    // ---- 통제 권한 ----

    /// <summary>붕괴 한계의 몇 %를 넘으면 Strained 로 표시할지.</summary>
    public int StrainedThresholdPercent { get; }

    // ---- 숙련 경험 ----

    /// <summary>원본 부하 1당 얻는 숙련 경험치.</summary>
    public int ExperiencePerLoadPoint { get; }

    /// <summary>사고(통제 상실) 발생 시 경험치에서 깎이는 비율.</summary>
    public int IncidentExperiencePenaltyPercent { get; }

    /// <summary>사고 발생 시 접객 종료 후에도 남는 후유증 부담.</summary>
    public AxisTriple IncidentResidualBurden { get; }

    // ---- 밤 ----

    public int CareReduction { get; }
    public int ManagedReleaseMinimumCollapse { get; }
    public int ManagedReleaseForcedIncrease { get; }
    public int ManagedReleaseRetainPercent { get; }
    public int ManagedReleaseExperience { get; }

    // ---- 캠페인 ----

    public int ServicesPerDay { get; }
    public int CampaignDayCount { get; }
    public int CampaignEnergyQuota { get; }

    public IReadOnlyList<CollapseMultiplierBand> MultiplierBands => _multiplierBands;

    public ProgressionTuning(
        IReadOnlyList<CollapseMultiplierBand> multiplierBands,
        IReadOnlyList<int> physicalMasteryThresholds,
        IReadOnlyList<int> mentalMasteryThresholds,
        IReadOnlyList<int> empathicMasteryThresholds,
        int aptitudeMitigationPerPoint,
        int minimumAppliedLoad,
        int strainedThresholdPercent,
        int experiencePerLoadPoint,
        int incidentExperiencePenaltyPercent,
        AxisTriple incidentResidualBurden,
        int careReduction,
        int managedReleaseMinimumCollapse,
        int managedReleaseForcedIncrease,
        int managedReleaseRetainPercent,
        int managedReleaseExperience,
        int servicesPerDay,
        int campaignDayCount,
        int campaignEnergyQuota)
    {
        _multiplierBands = SortedCopy(multiplierBands);

        _masteryThresholdsByAxis = new int[BurdenAxes.Count][];
        _masteryThresholdsByAxis[(int)BurdenAxis.Physical] = Copy(physicalMasteryThresholds);
        _masteryThresholdsByAxis[(int)BurdenAxis.Mental] = Copy(mentalMasteryThresholds);
        _masteryThresholdsByAxis[(int)BurdenAxis.Empathic] = Copy(empathicMasteryThresholds);

        AptitudeMitigationPerPoint = aptitudeMitigationPerPoint;
        MinimumAppliedLoad = minimumAppliedLoad;
        StrainedThresholdPercent = strainedThresholdPercent;
        ExperiencePerLoadPoint = experiencePerLoadPoint;
        IncidentExperiencePenaltyPercent = incidentExperiencePenaltyPercent;
        IncidentResidualBurden = incidentResidualBurden;
        CareReduction = careReduction;
        ManagedReleaseMinimumCollapse = managedReleaseMinimumCollapse;
        ManagedReleaseForcedIncrease = managedReleaseForcedIncrease;
        ManagedReleaseRetainPercent = managedReleaseRetainPercent;
        ManagedReleaseExperience = managedReleaseExperience;
        ServicesPerDay = servicesPerDay <= 0 ? 3 : servicesPerDay;
        CampaignDayCount = campaignDayCount <= 0 ? 3 : campaignDayCount;
        CampaignEnergyQuota = campaignEnergyQuota;
    }

    /// <summary>해당 축에서 level -> level+1 로 올라가기 위해 필요한 누적 경험치.</summary>
    public int GetMasteryThreshold(BurdenAxis axis, int level)
    {
        int[] thresholds = _masteryThresholdsByAxis[(int)axis];

        if (thresholds.Length == 0)
            return int.MaxValue;

        if (level < 0)
            level = 0;

        if (level >= thresholds.Length)
            return int.MaxValue;

        return thresholds[level];
    }

    public int GetMaxMasteryLevel(BurdenAxis axis)
        => _masteryThresholdsByAxis[(int)axis].Length;

    /// <summary>
    /// 버티컬 슬라이스 기본값. 배율 구간과 밤 처리 수치는 기획서 표기를 그대로 따른다.
    /// 완화량과 관리 붕괴 진입선은 3일 9접객 안에서 x2.0 구간과 관리 붕괴가 실제로 발생하도록 맞춘 값이다.
    /// 내장 콘텐츠 기준으로 무난한 플레이는 기준 에너지에 근소하게 미달한다.
    /// </summary>
    public static ProgressionTuning CreateDefault()
    {
        return new ProgressionTuning(
            multiplierBands: new[]
            {
                new CollapseMultiplierBand(0, 1.0f, "안정"),
                new CollapseMultiplierBand(25, 1.5f, "동요"),
                new CollapseMultiplierBand(50, 2.0f, "침식"),
                new CollapseMultiplierBand(75, 3.0f, "한계"),
            },
            physicalMasteryThresholds: new[] { 50, 120, 220 },
            mentalMasteryThresholds: new[] { 40, 100, 190 },
            empathicMasteryThresholds: new[] { 30, 80, 160 },
            aptitudeMitigationPerPoint: 1,
            minimumAppliedLoad: 1,
            strainedThresholdPercent: 85,
            experiencePerLoadPoint: 1,
            incidentExperiencePenaltyPercent: 60,
            incidentResidualBurden: new AxisTriple(5, 5, 5),
            careReduction: 20,
            managedReleaseMinimumCollapse: 50,
            managedReleaseForcedIncrease: 25,
            managedReleaseRetainPercent: 50,
            managedReleaseExperience: 20,
            servicesPerDay: 3,
            campaignDayCount: 3,
            campaignEnergyQuota: 120);
    }

    private static int[] Copy(IReadOnlyList<int> source)
    {
        if (source == null)
            return Array.Empty<int>();

        int[] copied = new int[source.Count];

        for (int i = 0; i < source.Count; i++)
            copied[i] = source[i];

        return copied;
    }

    private static CollapseMultiplierBand[] SortedCopy(IReadOnlyList<CollapseMultiplierBand> source)
    {
        if (source == null || source.Count == 0)
            return new[] { new CollapseMultiplierBand(0, 1.0f, "안정") };

        CollapseMultiplierBand[] copied = new CollapseMultiplierBand[source.Count];

        for (int i = 0; i < source.Count; i++)
            copied[i] = source[i];

        Array.Sort(copied, static (a, b) => a.MinCollapse.CompareTo(b.MinCollapse));

        return copied;
    }
}
