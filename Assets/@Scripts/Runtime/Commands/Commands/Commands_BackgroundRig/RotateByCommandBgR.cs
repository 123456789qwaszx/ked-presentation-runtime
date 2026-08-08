using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig Motion",
    "Rotate By",
    Order = -168)]
public sealed class RotateByCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Rotation;

    [Header("Rotation Delta")]
    public Vector3 deltaEuler = Vector3.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class RotateByCommandBgR : ClaimTweenCommandBase
{
    private readonly RotateByCommandSpecBgR _spec;

    private RectTransform _rect;

    private Vector3 _startEuler;
    private Vector3 _destEuler;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 진행률(0→1) 트윈이라 가속 재시작이 처음부터 다시 도는 것이다 —
    // 스텝 경계에서는 종전대로 즉시 확정한다.
    protected override bool AccelerateOnStepFinish => false;

    public RotateByCommandBgR(RotateByCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override bool TryResolveTargets(CommandRunScope scope)
    {
        scope.BackgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        _rect = rig?.GetRect(_spec.target);

        return _rect != null;
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);

        _startEuler = _rect.localEulerAngles;
        _destEuler = _startEuler + _spec.deltaEuler;
    }

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(() => 0f, ApplyProgress, 1f, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.localRotation = Quaternion.Euler(_destEuler);
    }

    protected override void MeasureTweenDistances(out float originalDistance, out float remainingDistance)
    {
        // AccelerateOnStepFinish = false라 불리지 않지만, 계약은 정직하게 채운다.
        originalDistance = _spec.deltaEuler.magnitude;
        remainingDistance = 0f;
    }

    private void ApplyProgress(float progress)
    {
        Vector3 euler = Vector3.LerpUnclamped(_startEuler, _destEuler, progress);
        _rect.localRotation = Quaternion.Euler(euler);
    }
}
