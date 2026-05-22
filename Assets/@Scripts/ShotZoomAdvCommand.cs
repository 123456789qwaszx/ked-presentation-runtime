using DG.Tweening;
using UnityEngine;

public sealed class ShotZoomCommandAdv : ShotIntentCommandBase<ShotZoomCommandSpec>
{
    protected override float Duration => Spec.duration;
    protected override Ease Ease => Spec.ease;
    protected override bool KillTween => Spec.killTween;

    public ShotZoomCommandAdv(PresentationResponseRig rig, ShotZoomCommandSpec spec) : base(rig, spec) { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from, 
        CommandRunScope scope)
    {
        return new PresentationIntentState
        {
            zoom = Mathf.Clamp(Spec.zoom, -10f, 10f),
            pan = from.pan,
            focusPoint = from.focusPoint,
        };
    }
}