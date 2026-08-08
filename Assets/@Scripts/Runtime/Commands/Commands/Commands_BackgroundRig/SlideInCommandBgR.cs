using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Background Rig Motion", "Slide In", Order = -771)]
public sealed class SlideInCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Track_Move;

    [Header("Slide")]
    public CharRigDirection direction = CharRigDirection.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("(overshoot that settles back)")]
    [Tooltip("0이면 일반 SlideIn에 가까워짐.")]
    public float punch = 24f;
}

public sealed class SlideInCommandBgR : SlideCommandBase
{
    private readonly SlideInCommandSpecBgR _spec;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    protected override Ease SlideEase => _spec.ease;
    protected override CharRigDirection SlideDirection => _spec.direction;
    protected override float SlideDistance => _spec.distance;
    protected override float Punch => _spec.punch;

    protected override bool CurrentPositionIsDestination => true;

    public SlideInCommandBgR(SlideInCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override float Bump(float easedProgress) => BumpTowardEnd(easedProgress);

    protected override RectTransform ResolveSlideRect(CommandRunScope scope)
    {
        scope.BackgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);

        return rig?.GetRect(_spec.target);
    }
}
