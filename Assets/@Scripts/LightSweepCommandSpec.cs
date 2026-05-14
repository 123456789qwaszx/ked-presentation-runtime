using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Light Sweep",
    Order = -847)]
public sealed class LightSweepCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Direction")]
    public LightSweepDirection direction = LightSweepDirection.LeftToRight;

    [Header("Shape")]
    public float broadGlowWidth = 960f;
    public float coreWidth = 180f;
    public float trailGlowWidth = 680f;
    public float slantPixels = 360f;
    public float extraTravel = 760f;

    [Header("Light")]
    public Color color = new Color(1f, 0.94f, 0.72f, 1f);

    [Range(0f, 1f)]
    public float broadGlowAlpha = 0.34f;

    [Range(0f, 1f)]
    public float coreAlpha = 1f;

    [Range(0f, 1f)]
    public float trailGlowAlpha = 0.24f;

    [Range(0f, 1f)]
    public float flashAlpha = 0.68f;

    [Header("Timing")]
    [Range(0f, 1f)]
    public float broadStart = 0f;

    [Range(0f, 1f)]
    public float broadEnd = 0.20f;

    [Range(0f, 1f)]
    public float coreStart = 0.20f;

    [Range(0f, 1f)]
    public float coreEnd = 0.55f;

    [Range(0f, 1f)]
    public float flashStart = 0.42f;

    [Range(0f, 1f)]
    public float flashPeak = 0.52f;

    [Range(0f, 1f)]
    public float flashEnd = 0.62f;

    [Range(0f, 1f)]
    public float trailStart = 0.55f;

    [Range(0f, 1f)]
    public float trailEnd = 1f;

    [Header("Tween")]
    public float duration = 0.72f;
    public Ease ease = Ease.Linear;

    [Header("Options")]
    public bool killTween = true;
    public bool disableOnComplete = true;
    public bool blockRaycastWhileSweeping = false;
}

public sealed class LightSweepCommand : CommandBase, IStepScopedCommand
{
    private readonly LightSweepCommandSpec _spec;

    private LightSweepGraphic _graphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public LightSweepCommand(LightSweepCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_graphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(_graphic, true);

        ApplyConfig();

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking)
        {
            CommitFinalState();
            yield break;
        }

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _graphic.gameObject.SetActive(true);
        ApplyTimeline(0f);

        _tween = DOTween
            .To(
                () => 0f,
                value =>
                {
                    if (!_canCommitFinalState || _graphic == null)
                        return;

                    ApplyTimeline(value);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_graphic)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _graphic == null)
                    return;

                CommitFinalState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_graphic == null)
            return;

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _graphic == null)
            return;

        _tween?.Kill(false);
        DOTween.Kill(_graphic, false);

        CommitFinalState();
    }

    private void ApplyTimeline(float t)
    {
        if (_graphic == null)
            return;

        float broad01 = Range01(t, _spec.broadStart, _spec.broadEnd);
        float core01 = Range01(t, _spec.coreStart, _spec.coreEnd);
        float trail01 = Range01(t, _spec.trailStart, _spec.trailEnd);
        float flash01 = Bell01(t, _spec.flashStart, _spec.flashPeak, _spec.flashEnd);

        broad01 = EaseOutCubic01(broad01);
        core01 = EaseInOutSine01(core01);
        trail01 = EaseOutCubic01(trail01);
        flash01 = EaseOutSine01(flash01);

        _graphic.SetLayerProgress(
            broad01,
            core01,
            trail01,
            flash01);
    }

    private void CommitFinalState()
    {
        if (_graphic != null)
        {
            ApplyConfig();

            _graphic.SetLayerProgress(1f, 1f, 1f, 0f);

            if (_spec.disableOnComplete)
            {
                _graphic.SetLayerProgress(0f, 0f, 0f, 0f);
                _graphic.gameObject.SetActive(false);
            }
        }

        _canCommitFinalState = false;
        _graphic = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        RectTransform rect = PresentationTargetResolver.ResolveRect(
            scope,
            _spec.target,
            _spec.strict,
            nameof(LightSweepCommand));

        if (rect == null)
            return;

        _graphic = rect.GetComponent<LightSweepGraphic>();

        if (_graphic == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[LightSweepCommand] Target '{_spec.target}' does not have LightSweepGraphic.");
        }
    }

    private void ApplyConfig()
    {
        if (_graphic == null)
            return;

        _graphic.color = _spec.color;

        _graphic.Configure(
            _spec.broadGlowWidth,
            _spec.coreWidth,
            _spec.trailGlowWidth,
            _spec.slantPixels,
            _spec.extraTravel,
            _spec.direction,
            _spec.broadGlowAlpha,
            _spec.coreAlpha,
            _spec.trailGlowAlpha,
            _spec.flashAlpha);

        _graphic.RaycastBlocking = _spec.blockRaycastWhileSweeping;
    }

    private static float Range01(float value, float start, float end)
    {
        if (end <= start)
            return value >= end ? 1f : 0f;

        return Mathf.Clamp01((value - start) / (end - start));
    }

    private static float Bell01(float value, float start, float peak, float end)
    {
        if (value <= start || value >= end)
            return 0f;

        if (value <= peak)
            return Range01(value, start, peak);

        return 1f - Range01(value, peak, end);
    }

    private static float EaseInOutSine01(float value)
    {
        value = Mathf.Clamp01(value);
        return -(Mathf.Cos(Mathf.PI * value) - 1f) * 0.5f;
    }

    private static float EaseOutSine01(float value)
    {
        value = Mathf.Clamp01(value);
        return Mathf.Sin(value * Mathf.PI * 0.5f);
    }

    private static float EaseOutCubic01(float value)
    {
        value = Mathf.Clamp01(value);
        value = 1f - value;
        return 1f - value * value * value;
    }
}