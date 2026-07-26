/// <summary>
/// 숙련 경험치 산출.
///
/// 경험치는 완화 후가 아니라 '몬스터가 실제로 가한 부담'인 원본 부하를 기준으로 한다.
/// 다만 통제 상실까지 간 접객은 끝까지 관리된 업무로 보지 않으므로 크게 깎인다.
/// </summary>
public static class MasteryExperienceRule
{
    public static AxisTriple CalculateGain(
        AxisTriple rawLoad,
        bool isIncident,
        ProgressionTuning tuning)
    {
        AxisTriple gain = new(
            rawLoad.Physical * tuning.ExperiencePerLoadPoint,
            rawLoad.Mental * tuning.ExperiencePerLoadPoint,
            rawLoad.Empathic * tuning.ExperiencePerLoadPoint);

        if (!isIncident)
            return gain;

        int retainPercent = 100 - tuning.IncidentExperiencePenaltyPercent;

        if (retainPercent < 0)
            retainPercent = 0;

        return gain.ScalePercent(retainPercent);
    }

    public static void Grant(MaidRuntimeState maid, AxisTriple gain)
    {
        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            BurdenAxis axis = BurdenAxes.FromIndex(i);
            maid.GetMastery(axis).AddExperience(gain[axis]);
        }
    }
}
