using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig Motion",
    "Rotate (From → To)",
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

public sealed class RotateToCommandBgR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly RotateToCommandSpecBgR _spec;

    private RectTransform _rect;

    private Vector3 _startEuler;
    private Vector3 _targetEuler;

    private Tween _tween;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public RotateToCommandBgR(RotateToCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.overrideFromEuler)
            _rect.localEulerAngles = _spec.fromEuler;

        CaptureTweenEndpoints();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOLocalRotate(_targetEuler, _spec.duration, RotateMode.Fast)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
        {
            ClaimTarget();

            if (_spec.overrideFromEuler)
                _rect.localEulerAngles = _spec.fromEuler;

            CaptureTweenEndpoints();
        }

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        scope.BackgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);
        HasClaimedTarget = true;
    }

    private void CaptureTweenEndpoints()
    {
        _startEuler = _rect.localEulerAngles;
        _targetEuler = ResolveTargetEuler(_startEuler);
    }

    private Vector3 ResolveTargetEuler(Vector3 startEuler)
    {
        if (!_spec.relativeToCurrent)
            return _spec.toEuler;

        return startEuler + _spec.toEuler;
    }

    private void CommitFinalState()
    {
        _rect.localEulerAngles = _targetEuler;

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        _tween = _rect
            .DOLocalRotate(_targetEuler, duration, RotateMode.Fast)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = EulerDistance(_startEuler, _targetEuler);
        float remainingDistance = EulerDistance(_rect.localEulerAngles, _targetEuler);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    private static float EulerDistance(Vector3 from, Vector3 to)
    {
        float x = Mathf.DeltaAngle(from.x, to.x);
        float y = Mathf.DeltaAngle(from.y, to.y);
        float z = Mathf.DeltaAngle(from.z, to.z);

        return new Vector3(x, y, z).magnitude;
    }

    #endregion
}