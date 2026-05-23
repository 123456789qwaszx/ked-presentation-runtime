using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom", Order = -850)]
public sealed class ShotZoomCommandSpec : CommandSpecBase
{
    [Header("Zoom")]
    [Tooltip("목표 zoom intent 값. 현재 pan/focusPoint는 유지합니다.")]
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

public sealed class ShotZoomCommand : ShotIntentCommandBase<ShotZoomCommandSpec>
{
    protected override float Duration => Spec.duration;
    protected override Ease Ease => Spec.ease;
    protected override bool KillTween => Spec.killTween;

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