using System;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Track", Order = -848)]
public sealed class ShotTrackCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    [Tooltip("현재 pan에 더할 값")]
    public Vector2 pan = Vector2.zero;
}

public sealed class ShotTrackCommand : ShotIntentCommandBase<ShotTrackCommandSpec>
{
    public ShotTrackCommand(PresentationShotResponseSystem rig, ShotTrackCommandSpec spec)
        : base(rig, spec)
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return new PresentationIntentState
        {
            zoom = from.zoom,
            panInRigSpace = from.panInRigSpace + spec.pan,
            focusPointInRigSpace = from.focusPointInRigSpace,
        };
    }
}