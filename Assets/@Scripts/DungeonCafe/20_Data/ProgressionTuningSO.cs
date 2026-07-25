using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ProgressionTuning",
    menuName = "Guesthouse/Progression Tuning")]
public sealed class ProgressionTuningSO : ScriptableObject
{
    [Header("Collapse Multiplier")]
    [SerializeField]
    private CollapseMultiplierBand[] multiplierBands =
    {
        new(0, 1.0f, "안정"),
        new(25, 1.5f, "동요"),
        new(50, 2.0f, "침식"),
        new(75, 3.0f, "한계"),
    };

    [Header("Mastery Thresholds (누적)")]
    [SerializeField] private int[] physicalMasteryThresholds = { 50, 120, 220 };
    [SerializeField] private int[] mentalMasteryThresholds = { 40, 100, 190 };
    [SerializeField] private int[] empathicMasteryThresholds = { 30, 80, 160 };

    [Header("Burden Accrual")]
    [SerializeField, Min(0)] private int aptitudeMitigationPerPoint = 1;
    [SerializeField, Min(0)] private int minimumAppliedLoad = 1;
    [SerializeField, Range(1, 100)] private int strainedThresholdPercent = 85;

    [Header("Mastery Experience")]
    [SerializeField, Min(0)] private int experiencePerLoadPoint = 1;
    [SerializeField, Range(0, 100)] private int incidentExperiencePenaltyPercent = 60;
    [SerializeField] private AxisTriple incidentResidualBurden = AxisTriple.Uniform(5);

    [Header("Night")]
    [SerializeField, Min(0)] private int careReduction = 20;
    [SerializeField, Min(0)] private int managedReleaseMinimumCollapse = 50;
    [SerializeField, Min(0)] private int managedReleaseForcedIncrease = 25;
    [SerializeField, Range(0, 100)] private int managedReleaseRetainPercent = 50;
    [SerializeField, Min(0)] private int managedReleaseExperience = 20;

    [Header("Campaign")]
    [SerializeField, Min(1)] private int servicesPerDay = 3;
    [SerializeField, Min(1)] private int campaignDayCount = 3;
    [SerializeField, Min(0)] private int campaignEnergyQuota = 120;

    public ProgressionTuning BuildTuning()
    {
        return new ProgressionTuning(
            multiplierBands,
            physicalMasteryThresholds,
            mentalMasteryThresholds,
            empathicMasteryThresholds,
            aptitudeMitigationPerPoint,
            minimumAppliedLoad,
            strainedThresholdPercent,
            experiencePerLoadPoint,
            incidentExperiencePenaltyPercent,
            incidentResidualBurden,
            careReduction,
            managedReleaseMinimumCollapse,
            managedReleaseForcedIncrease,
            managedReleaseRetainPercent,
            managedReleaseExperience,
            servicesPerDay,
            campaignDayCount,
            campaignEnergyQuota);
    }
}
