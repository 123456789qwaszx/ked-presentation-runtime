using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum TransitionPlayMode
{
    CoverOnly,
    UncoverOnly,
    CoverThenUncover,
}

[Serializable]
[CommandMenuHint("Transition", "Play Transition", Order = -950)]
public sealed class TransitionCommandSpec : CommandSpecBase
{
    [Header("Mode")]
    public TransitionPlayMode playMode = TransitionPlayMode.CoverThenUncover;

    [Header("Target")]
    public TransitionTargetKind targetKind = TransitionTargetKind.Blackout;
    public string customTargetKey = "";

    [Header("Opacity")]
    [Range(0f, 1f)]
    public float coveredAlpha = 1f;

    [Range(0f, 1f)]
    public float uncoveredAlpha = 0f;

    [Header("Durations")]
    [Min(0f)]
    public float coverDuration = 0.20f;

    [Min(0f)]
    public float uncoverDuration = 0.20f;

    [Tooltip("CoverThenUncover 모드에서 닫힌 뒤 유지할 시간.")]
    [Min(0f)]
    public float holdCoveredSeconds = 0f;

    [Header("Ease")]
    public Ease coverEase = Ease.OutCubic;
    public Ease uncoverEase = Ease.OutCubic;

    [Header("Playback")]
    public bool wait = true;
    public bool resetToOpenAtStart = true;
}

public sealed class TransitionCommand : CommandBase
{
    private readonly TransitionCommandSpec _spec;
    private readonly TransitionTargetRouter _targetRouter;

    private TransitionTargetHandle _target;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionCommand(
        TransitionTargetRouter targetRouter,
        TransitionCommandSpec spec)
    {
        _targetRouter = targetRouter;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveTarget();

        if (_target == null || !_target.IsValid)
            yield break;

        CanvasGroup cg = _target.canvasGroup;
        cg.DOKill(false);

        if (_spec.resetToOpenAtStart &&
            (_spec.playMode == TransitionPlayMode.CoverOnly ||
             _spec.playMode == TransitionPlayMode.CoverThenUncover))
        {
            SnapAlpha(cg, _spec.uncoveredAlpha, false);
        }

        if (scope.IsSkipping)
        {
            ApplySkipInstant(cg, _spec.playMode);
            yield break;
        }

        switch (_spec.playMode)
        {
            case TransitionPlayMode.CoverOnly:
                yield return PlayFade(cg, _spec.coveredAlpha, _spec.coverDuration, _spec.coverEase, false);
                
                if (_spec.holdCoveredSeconds > 0f)
                    yield return WaitUnscaled(_spec.holdCoveredSeconds);
                break;

            case TransitionPlayMode.UncoverOnly:
                yield return PlayFade(cg, _spec.uncoveredAlpha, _spec.uncoverDuration, _spec.uncoverEase, false);
                break;

            case TransitionPlayMode.CoverThenUncover:
                yield return PlayFade(cg, _spec.coveredAlpha, _spec.coverDuration, _spec.coverEase, false);

                if (_spec.holdCoveredSeconds > 0f)
                    yield return WaitUnscaled(_spec.holdCoveredSeconds);

                yield return PlayFade(cg, _spec.uncoveredAlpha, _spec.uncoverDuration, _spec.uncoverEase, false);
                break;
        }
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveTarget();

        if (_target == null || !_target.IsValid)
            return;

        CanvasGroup cg = _target.canvasGroup;
        cg.DOKill(false);

        ApplySkipInstant(cg, _spec.playMode);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveTarget();

        if (_target == null || !_target.IsValid)
            return;

        _target.canvasGroup.DOKill(false);
    }

    private void ResolveTarget()
    {
        _resolveAttempted = true;

        _targetRouter.TryResolve(_spec.targetKind, _spec.customTargetKey,
            out _target);
    }

    private IEnumerator PlayFade(CanvasGroup cg, float toAlpha, float duration, Ease ease, bool blockRaycasts)
    {
        if (cg == null)
            yield break;

        cg.DOKill(false);
        cg.blocksRaycasts = blockRaycasts;
        cg.interactable = false;

        if (duration <= 0f)
        {
            cg.alpha = Mathf.Clamp01(toAlpha);
            yield break;
        }

        Tween tween = cg
            .DOFade(Mathf.Clamp01(toAlpha), duration)
            .SetEase(ease)
            .SetUpdate(true);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    private void ApplySkipInstant(CanvasGroup cg, TransitionPlayMode mode)
    {
        switch (mode)
        {
            case TransitionPlayMode.CoverOnly:
                SnapAlpha(cg, _spec.coveredAlpha, false);
                break;

            case TransitionPlayMode.UncoverOnly:
                SnapAlpha(cg, _spec.uncoveredAlpha, false);
                break;

            case TransitionPlayMode.CoverThenUncover:
                SnapAlpha(cg, _spec.coveredAlpha, false);
                SnapAlpha(cg, _spec.uncoveredAlpha, false);
                break;
        }
    }

    private void SnapAlpha(CanvasGroup cg, float alpha, bool blockRaycasts)
    {
        if (cg == null)
            return;

        cg.DOKill(false);
        cg.alpha = Mathf.Clamp01(alpha);
        cg.blocksRaycasts = blockRaycasts;
        cg.interactable = false;
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}