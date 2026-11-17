using System;
using UnityEngine;

[Serializable]
public sealed class ShotDollyCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Dolly")]
    [Tooltip("카메라의 앞뒤 자리(절대값). 줌과 같은 눈금 — mid 면에서는 줌과 같은 크기이고, 먼 층은 덜 · 가까운 층은 더 커진다. +면 다가간다.")]
    [Range(-10f, 10f)]
    public float dolly = 0f;
}

// shot_dolly — 카메라의 몸을 앞뒤로 옮긴다. 층마다 다르게 커져 깊이가 난다.
public sealed class ShotDollyCommand : ShotIntentCommandBase<ShotDollyCommandSpec>
{
    public ShotDollyCommand(PresentationShotResponseSystem rig, ShotDollyCommandSpec spec)
        : base(rig, spec)
    { }

    protected override ShotAxes Axes => ShotAxes.Dolly;

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        return Ked.Presentation.Core.ShotIntents
            .Dolly(from.ToCore(), spec.dolly)
            .ToUnity();
    }
}
