using System;
using UnityEngine;

/// <summary>
/// 세 부담 축에 대한 정수 3벡터.
/// 부하량, 대응력, 붕괴도 스냅샷, 숙련 경험치 증가량 등 축 단위 수치를 공통으로 표현한다.
///
/// 값 타입이며 setter 를 제공하지 않는다. 변경은 항상 새 인스턴스를 만들어 반환한다.
/// 인스펙터 직렬화를 위해 readonly struct 대신 private serialized field 를 사용한다.
/// </summary>
[Serializable]
public struct AxisTriple : IEquatable<AxisTriple>
{
    [SerializeField] private int physical;
    [SerializeField] private int mental;
    [SerializeField] private int empathic;

    public int Physical => physical;
    public int Mental => mental;
    public int Empathic => empathic;

    public AxisTriple(int physical, int mental, int empathic)
    {
        this.physical = physical;
        this.mental = mental;
        this.empathic = empathic;
    }

    public static AxisTriple Zero => default;

    public static AxisTriple Uniform(int value) => new(value, value, value);

    public static AxisTriple FromAxis(BurdenAxis axis, int value)
        => Zero.WithAxis(axis, value);

    public int this[BurdenAxis axis] => axis switch
    {
        BurdenAxis.Physical => physical,
        BurdenAxis.Mental => mental,
        BurdenAxis.Empathic => empathic,
        _ => 0,
    };

    public int Total => physical + mental + empathic;

    public bool IsZero => Total == 0;

    /// <summary>가장 큰 축. 동률이면 Physical → Mental → Empathic 순으로 고정 선택한다.</summary>
    public BurdenAxis DominantAxis
    {
        get
        {
            BurdenAxis dominant = BurdenAxis.Physical;
            int best = physical;

            if (mental > best)
            {
                dominant = BurdenAxis.Mental;
                best = mental;
            }

            if (empathic > best)
                dominant = BurdenAxis.Empathic;

            return dominant;
        }
    }

    public AxisTriple WithAxis(BurdenAxis axis, int value) => axis switch
    {
        BurdenAxis.Physical => new AxisTriple(value, mental, empathic),
        BurdenAxis.Mental => new AxisTriple(physical, value, empathic),
        BurdenAxis.Empathic => new AxisTriple(physical, mental, value),
        _ => this,
    };

    public AxisTriple AddAxis(BurdenAxis axis, int delta)
        => WithAxis(axis, this[axis] + delta);

    public AxisTriple ScalePercent(int percent)
        => new(
            physical * percent / 100,
            mental * percent / 100,
            empathic * percent / 100);

    public static AxisTriple operator +(AxisTriple a, AxisTriple b)
        => new(
            a.physical + b.physical,
            a.mental + b.mental,
            a.empathic + b.empathic);

    public static AxisTriple operator -(AxisTriple a, AxisTriple b)
        => new(
            a.physical - b.physical,
            a.mental - b.mental,
            a.empathic - b.empathic);

    public static AxisTriple Max(AxisTriple a, AxisTriple b)
        => new(
            Math.Max(a.physical, b.physical),
            Math.Max(a.mental, b.mental),
            Math.Max(a.empathic, b.empathic));

    public bool Equals(AxisTriple other)
        => physical == other.physical
           && mental == other.mental
           && empathic == other.empathic;

    public override bool Equals(object obj) => obj is AxisTriple other && Equals(other);

    public override int GetHashCode() => (physical * 397 ^ mental) * 397 ^ empathic;

    public override string ToString() => $"({physical}/{mental}/{empathic})";
}
