/// <summary>
/// 밤 행동 규칙.
///
/// Care 는 붕괴도를 안전하게 낮추지만 효율이 낮다.
/// ManagedRelease 는 통제된 상태에서 메이드를 억압하는 것.
/// 붕괴를 피하는 것이 아니라, 관리 가능한 상대 앞에서 완료함으로써 회복시킨다는 것이 설계의 핵심이다.
/// 그래서 완전히 무너뜨리는 데 성공하면 수치가 오히려 절반으로 떨어진다.
/// </summary>
public static class NightConversionRule
{
    public static bool CanRunManagedRelease(
        MaidRuntimeState maid,
        BurdenAxis axis,
        ProgressionTuning tuning)
    {
        if (maid.IsLost)
            return false;

        return maid.Burden.Get(axis) >= tuning.ManagedReleaseMinimumCollapse;
    }

    public static NightProgramResult RunCare(
        MaidRuntimeState maid,
        BurdenAxis axis,
        ProgressionTuning tuning)
    {
        if (maid.IsLost)
            return NightProgramResult.Failed(NightProgramKind.Care, maid.MaidId, axis, "회수되지 않은 상태");

        int before = maid.Burden.Get(axis);
        maid.Burden.Reduce(axis, tuning.CareReduction);
        int after = maid.Burden.Get(axis);

        return NightProgramResult.Succeeded(
            NightProgramKind.Care,
            maid.MaidId,
            axis,
            collapseBefore: before,
            collapsePeak: before,
            collapseAfter: after,
            experienceGain: 0,
            isMasteryEventReady: maid.GetMastery(axis).IsEventReady(tuning));
    }

    public static NightProgramResult RunManagedRelease(
        MaidRuntimeState maid,
        BurdenAxis axis,
        ProgressionTuning tuning)
    {
        if (!CanRunManagedRelease(maid, axis, tuning))
        {
            return NightProgramResult.Failed(
                NightProgramKind.ManagedRelease,
                maid.MaidId,
                axis,
                $"{BurdenAxes.ToBurdenLabel(axis)} {tuning.ManagedReleaseMinimumCollapse} 미만");
        }

        int before = maid.Burden.Get(axis);

        // 낮에 억눌러 온 긴장을 관리 상태에서 한 번에 끌어올린다.
        int peak = System.Math.Min(
            maid.Burden.GetLimit(axis),
            before + tuning.ManagedReleaseForcedIncrease);

        maid.Burden.SetValue(axis, peak);

        // 끝까지 완료한 뒤 회수한다. 기준은 진입 시점의 붕괴도다.
        int after = before * tuning.ManagedReleaseRetainPercent / 100;
        maid.Burden.SetValue(axis, after);

        int experience = maid.GetMastery(axis).AddExperience(tuning.ManagedReleaseExperience);

        return NightProgramResult.Succeeded(
            NightProgramKind.ManagedRelease,
            maid.MaidId,
            axis,
            collapseBefore: before,
            collapsePeak: peak,
            collapseAfter: maid.Burden.Get(axis),
            experienceGain: experience,
            isMasteryEventReady: maid.GetMastery(axis).IsEventReady(tuning));
    }

    public static MasteryEventResult RunMasteryEvent(
        MaidRuntimeState maid,
        BurdenAxis axis,
        ProgressionTuning tuning,
        string eventNodeName)
    {
        MaidMasteryTrack track = maid.GetMastery(axis);

        if (!track.IsEventReady(tuning))
            return MasteryEventResult.NotReady(maid.MaidId, axis);

        int levelBefore = track.Level;
        int threshold = track.GetNextThreshold(tuning);

        track.CommitLevelUp(tuning);

        return MasteryEventResult.Committed(
            maid.MaidId,
            axis,
            levelBefore,
            track.Level,
            track.Experience,
            threshold,
            eventNodeName);
    }
}
