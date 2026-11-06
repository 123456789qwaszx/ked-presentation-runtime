using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class RotateByCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Rotation;

    [Header("Rotation Delta")]
    public Vector3 deltaEuler = Vector3.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class RotateByCommandCharR : RigClaimTweenCommandBase
{
    private readonly RotateByCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector3 _startEuler;
    private Vector3 _destEuler;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 진행률(0→1) 트윈이라 가속 재시작이 처음부터 다시 도는 것이다 —
    // 스텝 경계에서는 종전대로 즉시 확정한다.
    protected override bool AccelerateOnStepFinish => false;

    public RotateByCommandCharR(PresentationStage stage, RotateByCommandSpecCharR spec)
        : base(stage, spec.slotKey)
    {
        _spec = spec;
    }

    protected override void ResolveRects()
        => _rect = RigRefs.GetRect(_spec.target);

    protected override void ClaimTarget()
    {
        _rect.DOKill(true);

        _startEuler = _rect.localEulerAngles;
        _destEuler = _startEuler + _spec.deltaEuler;

        Claim(_rect, StageNodeClaim.LocalEuler(_rect.name, _destEuler.ToCore()));
    }

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(() => 0f, ApplyProgress, 1f, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void WriteFinalState()
        => _rect.localRotation = Quaternion.Euler(_destEuler);

    // AccelerateOnStepFinish = false라 불리지 않지만, 계약은 정직하게 채운다.
    protected override float MeasureRemainingRatio()
        => RemainingRatio(_spec.deltaEuler.magnitude, 0f);

    private void ApplyProgress(float progress)
    {
        Vector3 euler = Vector3.LerpUnclamped(_startEuler, _destEuler, progress);
        _rect.localRotation = Quaternion.Euler(euler);
    }
}
