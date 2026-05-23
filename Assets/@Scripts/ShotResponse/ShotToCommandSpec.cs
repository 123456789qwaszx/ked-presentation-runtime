using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot To", Order = -847)]
public sealed class ShotToCommandSpec : CommandSpecBase
{
    [Header("Pan")]
    public Vector2 pan = Vector2.zero;

    [Header("Zoom")]
    [Range(-10f, 10f)]
    public float zoom = 0f;

    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.45f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ShotToCommand : ShotIntentCommandBase<ShotToCommandSpec>
{
    protected override float Duration => Spec.duration;
    protected override Ease Ease => Spec.ease;
    protected override bool KillTween => Spec.killTween;

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
            zoom = PresentationShotIntentMath.ClampZoom(Spec.zoom),
            pan = Spec.pan,
            focusPoint = from.focusPoint,
        };
    }
}