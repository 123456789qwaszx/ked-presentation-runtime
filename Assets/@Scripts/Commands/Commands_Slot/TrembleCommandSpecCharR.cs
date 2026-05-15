using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Tremble", Order = -739)]
public sealed class TrembleCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Shake;

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

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class TrembleCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly TrembleCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _basePos;
    private float _seed;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TrembleCommandCharR(TrembleCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _basePos = _rect.anchoredPosition;
        _seed = UnityEngine.Random.Range(0f, 1000f);
        _canCommitFinalState = true;

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

        _tween = DOTween
            .To(
                () => 0f,
                elapsed =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

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
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
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

        if (_rect == null)
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

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.targetKey);

        _rect = rig.GetRect(_spec.target);

        if (_rect != null)
            _basePos = _rect.anchoredPosition;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _basePos;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private static float EvaluateEnvelope(
        float elapsed,
        float duration,
        float blendIn,
        float blendOut)
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