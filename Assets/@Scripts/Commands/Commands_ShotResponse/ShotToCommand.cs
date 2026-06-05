using System;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot To", Order = -847)]
public sealed class ShotToCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    public Vector2 pan = Vector2.zero;

    [Header("Zoom")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotToCommand : ShotIntentCommandBase<ShotToCommandSpec>
{
    public ShotToCommand(
        PresentationResponseRig rig,
        ShotToCommandSpec spec)
        : base(rig, spec)
    {
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return new PresentationIntentState
        {
            zoom = spec.zoom,
            pan = spec.pan,
            focusPoint = from.focusPoint,
        };
    }
}