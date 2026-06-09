using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Background Rig Motion", "Tremble", Order = -739)]
public sealed class TrembleCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Shake;

    [Header("Tremble")]
    [Tooltip("흔들림 강도. 픽셀 단위.")]
    public float strength = 8f;

    [Tooltip("주 흔들림 방향.")]
    public CharRigDirection direction = CharRigDirection.Right;

    [Tooltip("전체 지속 시간.")]
    public float duration = 1.2f;

    [Tooltip("초당 떨림 횟수. 값이 높을수록 달달달 떠는 느낌.")]
    public float frequency = 24f;

    [Header("Shape")]
    [Tooltip("수직 보조 흔들림 비율. 0이면 한 축으로만 떱니다.")]
    [Range(0f, 1f)]
    public float crossAxisRatio = 0.35f;

    [Tooltip("불규칙한 떨림을 섞는 정도. 0이면 규칙적인 진동.")]
    [Range(0f, 1f)]
    public float noiseRatio = 0.25f;

    [Header("Pulse")]
    [Tooltip("체크하면 계속 떠는 대신, 일정 간격마다 짧게 떱니다.")]
    public bool usePulse = false;

    [Tooltip("몇 초마다 한 번씩 떨지.")]
    public float pulseInterval = 1.0f;

    [Tooltip("한 번 떨 때 지속 시간.")]
    public float pulseDuration = 0.16f;

    [Header("Blend")]
    [Tooltip("시작할 때 흔들림이 켜지는 시간.")]
    public float blendIn = 0.04f;

    [Tooltip("끝날 때 원위치로 돌아가는 시간.")]
    public float blendOut = 0.08f;
}

public sealed class TrembleCommandBgR : CommandBase
{
    private readonly TrembleCommandSpecBgR _spec;

    private RectTransform _rect;
    private Vector2 _basePos;
    private float _seed;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public TrembleCommandBgR(TrembleCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            CommitFinalState();
            yield break;
        }

        Vector2 mainAxis = GetSignedDirection(_spec.direction);
        Vector2 crossAxis = new Vector2(-mainAxis.y, mainAxis.x);

        float strength = Mathf.Abs(_spec.strength);
        float frequency = Mathf.Max(0.01f, _spec.frequency);
        float crossRatio = Mathf.Clamp01(_spec.crossAxisRatio);
        float noiseRatio = Mathf.Clamp01(_spec.noiseRatio);

        Tween tween = DOTween
            .To(
                () => 0f,
                elapsed =>
                {
                    float envelope = _spec.usePulse
                        ? EvaluatePulseEnvelope(
                            elapsed,
                            _spec.duration,
                            _spec.pulseInterval,
                            _spec.pulseDuration,
                            _spec.blendIn,
                            _spec.blendOut)
                        : EvaluateEnvelope(
                            elapsed,
                            _spec.duration,
                            _spec.blendIn,
                            _spec.blendOut);

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
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rig = BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);

        _basePos = _rect.anchoredPosition;
        _seed = UnityEngine.Random.Range(0f, 1000f);

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _basePos;

        HasClaimedTarget = false;
    }

    private static float EvaluateEnvelope(float elapsed, float duration, float blendIn, float blendOut)
    {
        float inFactor = 1f;
        float outFactor = 1f;

        if (blendIn > 0f)
            inFactor = Mathf.Clamp01(elapsed / blendIn);

        if (blendOut > 0f)
            outFactor = Mathf.Clamp01((duration - elapsed) / blendOut);

        float factor = Mathf.Min(inFactor, outFactor);
        return Mathf.SmoothStep(0f, 1f, factor);
    }

    private static float EvaluatePulseEnvelope(
        float elapsed,
        float totalDuration,
        float pulseInterval,
        float pulseDuration,
        float blendIn,
        float blendOut)
    {
        if (elapsed < 0f || elapsed > totalDuration)
            return 0f;

        pulseInterval = Mathf.Max(0.01f, pulseInterval);
        pulseDuration = Mathf.Clamp(pulseDuration, 0.01f, pulseInterval);

        float cycleTime = Mathf.Repeat(elapsed, pulseInterval);

        if (cycleTime > pulseDuration)
            return 0f;

        float inFactor = 1f;
        float outFactor = 1f;

        if (blendIn > 0f)
            inFactor = Mathf.Clamp01(cycleTime / blendIn);

        if (blendOut > 0f)
            outFactor = Mathf.Clamp01((pulseDuration - cycleTime) / blendOut);

        float factor = Mathf.Min(inFactor, outFactor);
        return Mathf.SmoothStep(0f, 1f, factor);
    }

    private static Vector2 GetSignedDirection(CharRigDirection direction)
    {
        return direction switch
        {
            CharRigDirection.Left => Vector2.left,
            CharRigDirection.Right => Vector2.right,
            CharRigDirection.Up => Vector2.up,
            CharRigDirection.Down => Vector2.down,
            _ => Vector2.right,
        };
    }
}