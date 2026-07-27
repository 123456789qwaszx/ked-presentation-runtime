using System;
using UnityEngine;

/// <summary>
/// 결산 배율 구간 하나.
/// MinCollapse 이상이면 이 구간에 속하며, 밴드는 오름차순으로 정렬되어 평가된다.
/// </summary>
[Serializable]
public struct CollapseMultiplierBand
{
    [SerializeField] private int minCollapse;
    [SerializeField] private float multiplier;
    [SerializeField] private string label;

    public int MinCollapse => minCollapse;
    public float Multiplier => multiplier;
    public string Label => label;

    public CollapseMultiplierBand(int minCollapse, float multiplier, string label)
    {
        this.minCollapse = minCollapse;
        this.multiplier = multiplier;
        this.label = label;
    }

    public override string ToString() => $"{minCollapse}+ x{multiplier:0.0}";
}
