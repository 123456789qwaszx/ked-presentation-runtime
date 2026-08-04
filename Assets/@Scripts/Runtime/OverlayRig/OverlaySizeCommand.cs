using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Size",
    Order = -939)]
public sealed class OverlaySizeCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Target")]
    public OverlayRigTarget target = OverlayRigTarget.Overlay_Size;

    [Header("Size")]
    public bool relativeToCurrent = false;
    public Vector2 sizeDelta = Vector2.zero;

    [Header("Tween")]
    public float duration = 0f;
    public Ease ease = Ease.OutCubic;
}

public sealed class OverlaySizeCommand : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly OverlaySizeCommandSpec _spec;

    private OverlayRigRefs _refs;
    private RectTransform _rect;

    private Vector2 _startSize;
    private Vector2 _destSize;

    private Tween _tween;
    private bool _resolveAttempted;
    private bool _hasClaimedTarget;

    public override bool WaitForCompletion => _spec.wait;

    public OverlaySizeCommand(OverlaySizeCommandSpec spec)
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
            .DOSizeDelta(_destSize, duration)
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

        _startSize = _rect.sizeDelta;
        _destSize = _spec.relativeToCurrent
            ? _startSize + _spec.sizeDelta
            : _spec.sizeDelta;

        _hasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_refs == null)
            return;

        _refs.SetSizeDeltaImmediate(_spec.target, _destSize);

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
            .DOSizeDelta(_destSize, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = Vector2.Distance(_startSize, _destSize);
        float remainingDistance = Vector2.Distance(_rect.sizeDelta, _destSize);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }
}