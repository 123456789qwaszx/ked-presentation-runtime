using System;
using UnityEngine;

[Serializable]
public sealed class ShotRollCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Roll")]
    [Tooltip("화면의 기울기(도, 절대값). +면 그림이 반시계로 돈다. 모서리는 스스로 메우지 않는다 — 배경 여유가 없으면 줌 · 달리를 같이 건다.")]
    public float roll = 0f;
}

// shot_roll — 화면을 기울인다. 팬 안쪽의 것 전부가 화면 가운데를 중심으로 한 덩어리로 돈다.
public sealed class ShotRollCommand : ShotIntentCommandBase<ShotRollCommandSpec>
{
    public ShotRollCommand(PresentationShotResponseSystem rig, ShotRollCommandSpec spec)
        : base(rig, spec)
    { }

    protected override ShotAxes Axes => ShotAxes.Roll;

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        return Ked.Presentation.Core.ShotIntents
            .Roll(from.ToCore(), spec.roll)
            .ToUnity();
    }
}
