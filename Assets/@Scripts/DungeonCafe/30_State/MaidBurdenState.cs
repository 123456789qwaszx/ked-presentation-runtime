using System;

/// <summary>
/// 메이드의 현재 누적 부담(= 붕괴도).
/// 표기상 상처/스트레스/충동이며, 결산 배율과 통제 권한 판정이 모두 이 값을 본다.
/// 값은 항상 0..Limit 로 클램프되고, 한계에 도달한 축이 하나라도 있으면 통제 신호가 거부된다.
/// </summary>
public sealed class MaidBurdenState
{
    private readonly int[] _values = new int[BurdenAxes.Count];
    private readonly int[] _peaks = new int[BurdenAxes.Count];
    private readonly int[] _limits = new int[BurdenAxes.Count];

    public MaidBurdenState(AxisTriple limit)
    {
        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            BurdenAxis axis = BurdenAxes.FromIndex(i);
            _limits[i] = Math.Max(1, limit[axis]);
        }
    }

    public int Get(BurdenAxis axis) => _values[(int)axis];

    /// <summary>이번 회차 동안 기록된 최고 붕괴도. 엔딩 판정과 결과창 연출에 사용한다.</summary>
    public int GetPeak(BurdenAxis axis) => _peaks[(int)axis];

    public int GetLimit(BurdenAxis axis) => _limits[(int)axis];

    public AxisTriple Snapshot()
        => new(
            _values[(int)BurdenAxis.Physical],
            _values[(int)BurdenAxis.Mental],
            _values[(int)BurdenAxis.Empathic]);

    public AxisTriple PeakSnapshot()
        => new(
            _peaks[(int)BurdenAxis.Physical],
            _peaks[(int)BurdenAxis.Mental],
            _peaks[(int)BurdenAxis.Empathic]);

    public AxisTriple LimitSnapshot()
        => new(
            _limits[(int)BurdenAxis.Physical],
            _limits[(int)BurdenAxis.Mental],
            _limits[(int)BurdenAxis.Empathic]);

    /// <summary>실제로 누적된 양을 반환한다. 한계에 걸려 잘린 분량은 포함하지 않는다.</summary>
    public int Add(BurdenAxis axis, int amount)
    {
        if (amount <= 0)
            return 0;

        int index = (int)axis;
        int before = _values[index];
        int after = Math.Min(_limits[index], before + amount);

        _values[index] = after;

        if (after > _peaks[index])
            _peaks[index] = after;

        return after - before;
    }

    public AxisTriple Add(AxisTriple load)
    {
        return new AxisTriple(
            Add(BurdenAxis.Physical, load.Physical),
            Add(BurdenAxis.Mental, load.Mental),
            Add(BurdenAxis.Empathic, load.Empathic));
    }

    /// <summary>실제로 감소한 양을 반환한다.</summary>
    public int Reduce(BurdenAxis axis, int amount)
    {
        if (amount <= 0)
            return 0;

        int index = (int)axis;
        int before = _values[index];
        int after = Math.Max(0, before - amount);

        _values[index] = after;

        return before - after;
    }

    public void SetValue(BurdenAxis axis, int value)
    {
        int index = (int)axis;
        int clamped = Math.Clamp(value, 0, _limits[index]);

        _values[index] = clamped;

        if (clamped > _peaks[index])
            _peaks[index] = clamped;
    }

    public bool IsAtLimit(BurdenAxis axis) => _values[(int)axis] >= _limits[(int)axis];

    public bool TryFindLimitBreachAxis(out BurdenAxis axis)
    {
        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            if (_values[i] < _limits[i])
                continue;

            axis = BurdenAxes.FromIndex(i);
            return true;
        }

        axis = BurdenAxis.Physical;
        return false;
    }

    public int GetPercentOfLimit(BurdenAxis axis)
    {
        int index = (int)axis;
        return _values[index] * 100 / _limits[index];
    }

    public int HighestPercentOfLimit()
    {
        int highest = 0;

        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            int percent = _values[i] * 100 / _limits[i];

            if (percent > highest)
                highest = percent;
        }

        return highest;
    }

    public void ClearPeaks()
    {
        Array.Clear(_peaks, 0, _peaks.Length);
    }

    public void ResetAll()
    {
        Array.Clear(_values, 0, _values.Length);
        Array.Clear(_peaks, 0, _peaks.Length);
    }
}
