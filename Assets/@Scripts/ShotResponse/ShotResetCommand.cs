using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Reset", Order = -849)]
public sealed class ShotResetCommandSpec : CommandSpecBase
{
    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.35f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ShotResetCommand : ShotIntentCommandBase<ShotResetCommandSpec>
{
    protected override float Duration => Spec.duration;
    protected override Ease Ease => Spec.ease;
    protected override bool KillTween => Spec.killTween;

    public ShotResetCommand(
        PresentationResponseRig rig,
        ShotResetCommandSpec spec)
        : base(rig, spec)
    {
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return PresentationIntentState.Default;
    }
}