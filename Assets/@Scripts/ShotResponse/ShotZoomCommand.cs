using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom", Order = -850)]
public sealed class ShotZoomCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Zoom")]
    [Tooltip("목표 zoom intent 값. 현재 pan/focusPoint는 유지합니다.")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotZoomCommand : ShotIntentCommandBase<ShotZoomCommandSpec>
{
    public ShotZoomCommand(
        PresentationResponseRig rig,
        ShotZoomCommandSpec spec)
        : base(rig, spec)
    {
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return new PresentationIntentState
        {
            zoom = PresentationShotIntentMath.ClampZoom(Spec.zoom),
            pan = from.pan,
            focusPoint = from.focusPoint,
        };
    }
}