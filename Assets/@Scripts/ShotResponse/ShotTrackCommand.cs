using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Track", Order = -848)]
public sealed class ShotTrackCommandSpec : CommandSpecBase
{
    [Header("Pan")]
    [Tooltip("relative=true면 현재 pan에 더할 값, false면 목표 pan 값입니다.")]
    public Vector2 pan = Vector2.zero;

    [Tooltip("체크하면 현재 pan 기준으로 더합니다. 끄면 절대 pan 값으로 이동합니다.")]
    public bool relative = true;

    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.35f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ShotTrackCommand : ShotIntentCommandBase<ShotTrackCommandSpec>
{
    protected override float Duration => Spec.duration;
    protected override Ease Ease => Spec.ease;
    protected override bool KillTween => Spec.killTween;

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