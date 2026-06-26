using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Scale",
    Order = -938)]
public sealed class OverlayScaleCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Target")]
    public OverlayRigTarget target = OverlayRigTarget.Overlay_Scale;

    [Header("Scale")]
    public bool relativeToCurrent = false;
    public Vector2 scale = Vector2.one;

    [Header("Tween")]
    public float duration = 0f;
    public Ease ease = Ease.OutCubic;
}

public sealed class OverlayScaleCommand : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly OverlayScaleCommandSpec _spec;

    private OverlayRigRefs _refs;
    private RectTransform _rect;

    private Vector3 _startScale;
    private Vector3 _destScale;

    private Tween _tween;
    private bool _resolveAttempted;
    private bool _hasClaimedTarget;

    public override bool WaitForCompletion => _spec.wait;

    public OverlayScaleCommand(OverlayScaleCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_rect == null)
            yield break;

        float duration = scope.ScalePresentationDuration(_spec.duration);

        if (duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOScale(_destScale, duration)
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

        if (!_hasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.OverlayRigs.TryGet(_spec.rigKey, out _refs))
            return;

        _rect = _refs.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        if (_rect == null)
            return;

        _refs.KillTween(_spec.target, true);

        _startScale = _rect.localScale;

        Vector3 requested = new(
            _spec.scale.x,
            _spec.scale.y,
            1f);

        _destScale = _spec.relativeToCurrent
            ? new Vector3(
                _startScale.x * requested.x,
                _startScale.y * requested.y,
                _startScale.z)
            : requested;

        _hasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_refs == null)
            return;

        _refs.SetLocalScaleImmediate(_spec.target, _destScale);

        _hasClaimedTarget = false;
        _tween = null;
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!_hasClaimedTarget || _rect == null)
            return;

        _tween?.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        if (duration <= 0f)
        {
            CommitFinalState();
            return;
        }

        _tween = _rect
            .DOScale(_destScale, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = Vector3.Distance(_startScale, _destScale);
        float remainingDistance = Vector3.Distance(_rect.localScale, _destScale);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }
}