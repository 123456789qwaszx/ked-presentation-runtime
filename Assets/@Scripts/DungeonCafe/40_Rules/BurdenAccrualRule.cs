/// <summary>
/// 원본 부하를 대응력으로 완화한 뒤 붕괴도에 누적한다.
///
/// 완화는 부하를 0으로 만들지 않는다. 대응력이 아무리 높아도 최소치는 남으며,
/// 이것이 "이상적인 플레이일수록 붕괴도가 한계에 가까워진다"는 낮 설계의 전제다.
/// </summary>
public static class BurdenAccrualRule
{
    public static int MitigateAxis(int rawLoad, int aptitude, ProgressionTuning tuning)
    {
        if (rawLoad <= 0)
            return 0;

        int mitigated = rawLoad - aptitude * tuning.AptitudeMitigationPerPoint;

        return mitigated < tuning.MinimumAppliedLoad
            ? tuning.MinimumAppliedLoad
            : mitigated;
    }

    public static AxisTriple Mitigate(AxisTriple rawLoad, AxisTriple aptitude, ProgressionTuning tuning)
    {
        return new AxisTriple(
            MitigateAxis(rawLoad.Physical, aptitude.Physical, tuning),
            MitigateAxis(rawLoad.Mental, aptitude.Mental, tuning),
            MitigateAxis(rawLoad.Empathic, aptitude.Empathic, tuning));
    }

    /// <summary>실제로 붕괴도에 누적된 양을 반환한다. 한계에서 잘린 분량은 제외된다.</summary>
    public static AxisTriple Apply(
        MaidRuntimeState maid,
        AxisTriple rawLoad,
        ProgressionTuning tuning)
    {
        AxisTriple mitigated = Mitigate(rawLoad, maid.Aptitude, tuning);

        return maid.Burden.Add(mitigated);
    }
}
