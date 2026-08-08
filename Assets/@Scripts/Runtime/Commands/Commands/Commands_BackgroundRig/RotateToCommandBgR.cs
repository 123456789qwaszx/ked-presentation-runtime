using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig Motion",
    "Rotate (From -> To)",
    Order = -180)]
public sealed class RotateToCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Rotation;

    [Header("Rotation (localEulerAngles)")]
    public Vector3 toEuler = Vector3.zero;

    [Header("Mode")]
    [Tooltip("false면 toEuler를 절대 localEulerAngles로 사용합니다. true면 현재 localEulerAngles에 toEuler를 더합니다.")]
    public bool relativeToCurrent = false;

    [Header("From")]
    public bool overrideFromEuler = false;
    public Vector3 fromEuler = Vector3.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class RotateToCommandBgR : ClaimTweenCommandBase
{
    private readonly RotateToCommandSpecBgR _spec;

    private RectTransform _rect;

    private Vector3 _startEuler;
    private Vector3 _targetEuler;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public RotateToCommandBgR(RotateToCommandSpecBgR spec)
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

        if (_spec.overrideFromEuler)
            _rect.localEulerAngles = _spec.fromEuler;

        _startEuler = _rect.localEulerAngles;

        _targetEuler = _spec.relativeToCurrent
            ? _startEuler + _spec.toEuler
            : _spec.toEuler;
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOLocalRotate(_targetEuler, duration, RotateMode.Fast)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.localEulerAngles = _targetEuler;
    }

    protected override void MeasureTweenDistances(out float originalDistance, out float remainingDistance)
    {
        originalDistance = EulerDistance(_startEuler, _targetEuler);
        remainingDistance = EulerDistance(_rect.localEulerAngles, _targetEuler);
    }

    private static float EulerDistance(Vector3 from, Vector3 to)
    {
        float x = Mathf.DeltaAngle(from.x, to.x);
        float y = Mathf.DeltaAngle(from.y, to.y);
        float z = Mathf.DeltaAngle(from.z, to.z);

        return new Vector3(x, y, z).magnitude;
    }
}
