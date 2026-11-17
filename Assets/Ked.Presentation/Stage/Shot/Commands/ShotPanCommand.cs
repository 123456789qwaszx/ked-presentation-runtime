using System;
using UnityEngine;

[Serializable]
public sealed class ShotPanCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    [Tooltip("팬(화면 밀기)의 절대값. 화면이 가는 쪽이 +.")]
    public Vector2 pan = Vector2.zero;
}

// shot_pan — 팬만 바꾼다. 줌은 건드리지 않는다.
public sealed class ShotPanCommand : ShotIntentCommandBase<ShotPanCommandSpec>
{
    public ShotPanCommand(PresentationShotResponseSystem rig, ShotPanCommandSpec spec)
        : base(rig, spec)
    { }

    protected override ShotAxes Axes => ShotAxes.Pan;

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        return Ked.Presentation.Core.ShotIntents
            .Pan(from.ToCore(), spec.pan.ToCore())
            .ToUnity();
    }
}
