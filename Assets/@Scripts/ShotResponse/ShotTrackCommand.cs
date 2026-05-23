using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Track", Order = -848)]
public sealed class ShotTrackCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    [Tooltip("relative=true면 현재 pan에 더할 값, false면 목표 pan 값입니다.")]
    public Vector2 pan = Vector2.zero;

    [Tooltip("체크하면 현재 pan 기준으로 더합니다. 끄면 절대 pan 값으로 이동합니다.")]
    public bool relative = true;
}

public sealed class ShotTrackCommand : ShotIntentCommandBase<ShotTrackCommandSpec>
{
    public ShotTrackCommand(
        PresentationResponseRig rig,
        ShotTrackCommandSpec spec)
        : base(rig, spec)
    {
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        Vector2 targetPan = Spec.relative
            ? from.pan + Spec.pan
            : Spec.pan;

        return new PresentationIntentState
        {
            zoom = from.zoom,
            pan = targetPan,
            focusPoint = from.focusPoint,
        };
    }
}