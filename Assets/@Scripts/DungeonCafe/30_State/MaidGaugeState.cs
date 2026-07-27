using System;

/// <summary>
/// v3 붕괴 게이지: 축당 0~200. (§1)
/// 기존 MaidBurdenState(0~limit 클램프)를 대체한다.
/// 100 은 한계가 아니라 통제 상실 문턱이고, 하드 캡은 200 뿐이다.
/// </summary>
public sealed class MaidGaugeState
{
    private readonly int[] _values = new int[BurdenAxes.Count];
    private readonly int[] _peaks = new int[BurdenAxes.Count];
    private readonly int _hardCap;

    public MaidGaugeState(int hardCap) { _hardCap = Math.Max(1, hardCap); }

    public int Get(BurdenAxis axis) => _values[(int)axis];
    public int GetPeak(BurdenAxis axis) => _peaks[(int)axis];

    public AxisTriple Snapshot() => new(
        _values[(int)BurdenAxis.Physical], _values[(int)BurdenAxis.Mental], _values[(int)BurdenAxis.Empathic]);

    public AxisTriple PeakSnapshot() => new(
        _peaks[(int)BurdenAxis.Physical], _peaks[(int)BurdenAxis.Mental], _peaks[(int)BurdenAxis.Empathic]);

    /// <summary>실제 누적량 반환. 200 캡에서 잘린 분량 제외.</summary>
    public int Add(BurdenAxis axis, int amount)
    {
        if (amount <= 0) return 0;
        int i = (int)axis;
        int before = _values[i];
        int after = Math.Min(_hardCap, before + amount);
        _values[i] = after;
        if (after > _peaks[i]) _peaks[i] = after;
        return after - before;
    }

    public int Reduce(BurdenAxis axis, int amount)
    {
        if (amount <= 0) return 0;
        int i = (int)axis;
        int before = _values[i];
        _values[i] = Math.Max(0, before - amount);
        return before - _values[i];
    }

    public void SetValue(BurdenAxis axis, int value)
    {
        int i = (int)axis;
        int clamped = Math.Clamp(value, 0, _hardCap);
        _values[i] = clamped;
        if (clamped > _peaks[i]) _peaks[i] = clamped;
    }

    /// <summary>전 축 중 최고값과 그 축. 동률이면 육→정→감. 밤 판정 기준. (§6)</summary>
    public BurdenAxis HighestAxis(out int value)
    {
        BurdenAxis axis = BurdenAxis.Physical;
        value = _values[0];
        for (int i = 1; i < BurdenAxes.Count; i++)
            if (_values[i] > value) { value = _values[i]; axis = BurdenAxes.FromIndex(i); }
        return axis;
    }

    /// <summary>어떤 축이든 threshold 이상인 축 탐색. 심층 진입 판정. (§3)</summary>
    public bool TryFindAxisAtOrAbove(int threshold, out BurdenAxis axis)
    {
        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            if (_values[i] < threshold) continue;
            axis = BurdenAxes.FromIndex(i);
            return true;
        }
        axis = BurdenAxis.Physical;
        return false;
    }

    public void ClearPeaks() => Array.Clear(_peaks, 0, _peaks.Length);
}
