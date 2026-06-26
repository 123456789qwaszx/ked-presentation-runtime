using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Show",
    Order = -937)]
public sealed class OverlayShowCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Fade")]
    public float duration = 0.15f;
    public Ease ease = Ease.OutCubic;
}

public sealed class OverlayShowCommand : CommandBase
{
    private readonly OverlayShowCommandSpec _spec;

    private OverlayRigRefs _refs;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _hasClaimedTarget;

    public override bool WaitForCompletion => _spec.wait;

    public OverlayShowCommand(OverlayShowCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_refs?.Overlay_RootCanvasGroup == null)
            yield break;

        float duration = scope.ScalePresentationDuration(_spec.duration);

        if (duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _refs.Overlay_RootCanvasGroup
            .DOFade(1f, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_refs.Overlay_RootCanvasGroup)
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
        scope.OverlayRigs.TryGet(_spec.rigKey, out _refs);
    }

    private void ClaimTarget()
    {
        if (_refs?.Overlay_RootCanvasGroup == null)
            return;

        _refs.KillRootCanvasTween(true);
        _hasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _refs?.SetRootAlphaImmediate(1f);
        _hasClaimedTarget = false;
        _tween = null;
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!_hasClaimedTarget)
            return;

        _tween?.Kill(false);
        CommitFinalState();
    }
}