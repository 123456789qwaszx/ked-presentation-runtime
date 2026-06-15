
using System.Collections;
using DG.Tweening;
using UnityEngine;

public sealed class EmojiTrembleCommandCharR : CharacterEmojiCommandBase
{
    private readonly EmojiTrembleCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _rect;
    private Vector2 _basePos;
    private float _seed;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiTrembleCommandCharR(
        EmojiTrembleCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        CharacterEmojiMirrorContext context = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            _spec.emojiKey);

        ClaimTarget();

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            CommitFinalState();
            yield break;
        }

        Vector2 mainAxis = GetSignedDirection(context.MirrorDirection(_spec.direction));
        Vector2 crossAxis = new Vector2(-mainAxis.y, mainAxis.x);
        float strength = Mathf.Abs(_spec.strength);
        float frequency = Mathf.Max(0.01f, _spec.frequency);
        float crossRatio = Mathf.Clamp01(_spec.crossAxisRatio);
        float noiseRatio = Mathf.Clamp01(_spec.noiseRatio);

        _tween = DOTween
            .To(
                () => 0f,
                elapsed =>
                {
                    float envelope = _spec.usePulse
                        ? EvaluatePulseEnvelope(elapsed)
                        : EvaluateEnvelope(elapsed);

                    float phase = elapsed * frequency;
                    float mainWave = Mathf.Sin(phase * Mathf.PI * 2f);
                    float crossWave = Mathf.Sin((phase * 1.37f + 0.25f) * Mathf.PI * 2f);
                    float noiseA = Mathf.PerlinNoise(_seed, elapsed * frequency) * 2f - 1f;
                    float noiseB = Mathf.PerlinNoise(_seed + 17.3f, elapsed * frequency * 1.11f) * 2f - 1f;
                    float main = Mathf.Lerp(mainWave, noiseA, noiseRatio);
                    float cross = Mathf.Lerp(crossWave, noiseB, noiseRatio);

                    Vector2 offset =
                        mainAxis * (main * strength) +
                        crossAxis * (cross * strength * crossRatio);

                    _rect.anchoredPosition = _basePos + offset * envelope;
                },
                _spec.duration,
                _spec.duration)
            .SetEase(Ease.Linear)
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
            ClaimTarget();

        CommitFinalState();
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween?.Kill(false);
        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rigRefs.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);
        _basePos = _rect.anchoredPosition;
        _seed = UnityEngine.Random.value * 1000f;
        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _basePos;

        HasClaimedTarget = false;
        _tween = null;
    }

    private float EvaluateEnvelope(float elapsed)
    {
        float inT = _spec.blendIn <= 0f ? 1f : Mathf.Clamp01(elapsed / _spec.blendIn);
        float outT = _spec.blendOut <= 0f ? 1f : Mathf.Clamp01((_spec.duration - elapsed) / _spec.blendOut);
        return SmoothStep01(Mathf.Min(inT, outT));
    }

    private float EvaluatePulseEnvelope(float elapsed)
    {
        if (_spec.pulseInterval <= 0f)
            return EvaluateEnvelope(elapsed);

        float local = elapsed % _spec.pulseInterval;
        if (local > _spec.pulseDuration)
            return 0f;

        float inT = _spec.blendIn <= 0f ? 1f : Mathf.Clamp01(local / _spec.blendIn);
        float outT = _spec.blendOut <= 0f ? 1f : Mathf.Clamp01((_spec.pulseDuration - local) / _spec.blendOut);
        return SmoothStep01(Mathf.Min(inT, outT));
    }

    private static float SmoothStep01(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }
}
