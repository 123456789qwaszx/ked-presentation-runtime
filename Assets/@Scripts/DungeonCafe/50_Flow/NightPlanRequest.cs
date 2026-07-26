using System.Collections.Generic;

public sealed class NightPlanRequest
{
    public int DayNumber { get; }
    public IReadOnlyList<MaidRuntimeState> Maids { get; }
    public ProgressionTuning Tuning { get; }

    public NightPlanRequest(
        int dayNumber,
        IReadOnlyList<MaidRuntimeState> maids,
        ProgressionTuning tuning)
    {
        DayNumber = dayNumber;
        Maids = maids;
        Tuning = tuning;
    }

    public bool CanRunManagedRelease(MaidRuntimeState maid, BurdenAxis axis)
        => NightConversionRule.CanRunManagedRelease(maid, axis, Tuning);
}
