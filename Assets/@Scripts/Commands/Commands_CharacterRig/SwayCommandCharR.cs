using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Sway", Order = 100)]
public sealed class SwayCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Targets")]
    [Tooltip("좌우로 흔들릴 피벗(SwayPivot).")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_SwayPivot;

    [Header("Sway")]
    [FormerlySerializedAs("power")]
    [Tooltip("최대 회전 세기(도). NudgeTap의 strength와 같은 감각으로 사용.")]
    public float strength = 10f;

    [Tooltip("총 길이.")]
    public float duration = 1f;

    [Min(1)]
    [Tooltip("왕복 횟수. 1이면 한 번 갔다가 돌아오고, 2 이상이면 좌우 반복.")]
    public int cycles = 2;

    [FormerlySerializedAs("edgeDamping")]
    [Tooltip("각 half-swing에서 극단 감속/체류감을 만드는 값. 높을수록 더 오래 눌리듯 멈춘다.")]
    public float damping = 2.2f;

    [FormerlySerializedAs("swingSpeed")]
    [Tooltip("좌↔우 한 번 이동하는 체감 속도. 1=기본, 높을수록 더 날렵하고 낮을수록 더 둔하다.")]
    public float speed = 1f;

    [Tooltip("마지막에 원점으로 복귀할 때 살짝 지나쳤다가 돌아오는 정도. 0이면 비활성.")]
    [Range(0f, 1f)]
    public float finalOvershoot = 0.22f;

    [Header("Style")]
    [Tooltip("시작 전에 반대 방향으로 살짝 당기는 양(도). 0이면 비활성.")]
    public float anticipation = 0f;

    [Tooltip("흔들리는 시작 방향. true면 +방향부터, false면 -방향부터.")]
    public bool startPositive = true;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class SwayCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SwayCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private float _originSwayRotationZ;
    private float _currentSwayRotationZ;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SwayCommandCharR(SwayCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.
        
        _tween?.Kill(false);
        _tween = null;
        _canCommitFinalState = true;

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            SnapToOrigin();
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        float amplitude = Mathf.Abs(_spec.strength);
        float totalDuration = Mathf.Max(0.0001f, _spec.duration);
        int cycles = Mathf.Max(1, _spec.cycles);
        float damping = Mathf.Max(0f, _spec.damping);
        float speed = Mathf.Max(0.05f, _spec.speed);
        float finalOvershoot = Mathf.Clamp01(_spec.finalOvershoot);
        float anticipationDegrees = _spec.anticipation;
        float directionSign = _spec.startPositive ? 1f : -1f;

        float anticipationNormalized = Mathf.Approximately(amplitude, 0f)
            ? 0f
            : Mathf.Clamp(anticipationDegrees / amplitude, 0f, 1.5f);

        bool hasAnticipation = anticipationNormalized > 0.0001f;

        int pointCount = hasAnticipation
            ? (cycles * 2 + 2)
            : (cycles * 2 + 1);

        int segmentCount = pointCount - 1;
        int lastSegmentIndex = segmentCount - 1;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;
                    
                    float u = Mathf.Clamp01(t / totalDuration);

                    float pathT = u * segmentCount;
                    int segmentIndex = Mathf.Min(Mathf.FloorToInt(pathT), segmentCount - 1);
                    float localT = pathT - segmentIndex;

                    float shapedT = ShapeHalfSwing(localT, damping, speed);

                    if (segmentIndex == lastSegmentIndex && finalOvershoot > 0f)
                        shapedT = ApplyFinalOvershoot(shapedT, finalOvershoot);

                    float from = GetWavePointWithAnticipation(
                        segmentIndex,
                        pointCount,
                        anticipationNormalized,
                        hasAnticipation);

                    float to = GetWavePointWithAnticipation(
                        segmentIndex + 1,
                        pointCount,
                        anticipationNormalized,
                        hasAnticipation);

                    float wave = Mathf.LerpUnclamped(from, to, shapedT);
                    float angleOffset = directionSign * wave * amplitude;

                    _currentSwayRotationZ = _originSwayRotationZ + angleOffset;
                    SetLocalEulerZ(_rect, _currentSwayRotationZ);
                },
                totalDuration,
                totalDuration
            )
            .SetEase(Ease.Linear)
            .SetTarget(_rect)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                SnapToOrigin();
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
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

        SnapToOrigin();

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);

        SnapToOrigin();

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);


        _rect = rigRefs.GetRect(_spec.target);
        _originSwayRotationZ = NormalizeAngle(_rect.localEulerAngles.z);
        _currentSwayRotationZ = _originSwayRotationZ;
        SetLocalEulerZ(_rect, _currentSwayRotationZ);
    }

    private void SnapToOrigin()
    {
        _currentSwayRotationZ = _originSwayRotationZ;
        SetLocalEulerZ(_rect, _originSwayRotationZ);
    }

    private static float GetWavePointWithAnticipation(
        int pointIndex,
        int pointCount,
        float anticipationNormalized,
        bool hasAnticipation)
    {
        if (!hasAnticipation)
            return GetWavePoint(pointIndex, pointCount);

        if (pointIndex <= 0)
            return 0f;

        if (pointIndex >= pointCount - 1)
            return 0f;

        if (pointIndex == 1)
            return -anticipationNormalized;

        return (pointIndex % 2 == 0) ? 1f : -1f;
    }

    private static float GetWavePoint(int pointIndex, int pointCount)
    {
        if (pointIndex <= 0)
            return 0f;

        if (pointIndex >= pointCount - 1)
            return 0f;

        return (pointIndex % 2 == 1) ? 1f : -1f;
    }

    private static float ShapeHalfSwing(float t, float damping, float speed)
    {
        t = Mathf.Clamp01(t);

        float speedT = ApplySymmetricSpeedWarp(t, speed);
        float baseEase = EaseInOutCos(speedT);
        float damped = ApplyMildEdgeDamping(baseEase, damping);

        return Mathf.Clamp01(damped);
    }

    private static float ApplySymmetricSpeedWarp(float t, float speed)
    {
        t = Mathf.Clamp01(t);
        speed = Mathf.Max(0.05f, speed);

        if (Mathf.Approximately(speed, 1f))
            return t;

        float exponent;

        if (speed > 1f)
        {
            float fastBlend = 1f - Mathf.Exp(-(speed - 1f) * 0.9f);
            exponent = Mathf.Lerp(1f, 0.72f, fastBlend);
        }
        else
        {
            float slowBlend = 1f - Mathf.Exp(-(1f - speed) * 1.15f);
            exponent = Mathf.Lerp(1f, 1.45f, slowBlend);
        }

        if (t < 0.5f)
            return 0.5f * Mathf.Pow(t * 2f, exponent);

        return 1f - 0.5f * Mathf.Pow((1f - t) * 2f, exponent);
    }

    private static float ApplyMildEdgeDamping(float t, float damping)
    {
        t = Mathf.Clamp01(t);
        damping = Mathf.Max(0f, damping);

        if (damping <= 0.0001f)
            return t;

        float compressed = Mathf.Log(1f + damping) / Mathf.Log(2f);
        float blend = 1f - Mathf.Exp(-compressed * 0.22f);
        blend *= 0.72f;

        float holdEase = EaseInOutCos(t);
        holdEase = EaseInOutCos(holdEase);

        return Mathf.Lerp(t, holdEase, blend);
    }

    private static float ApplyFinalOvershoot(float t, float overshootAmount)
    {
        t = Mathf.Clamp01(t);
        overshootAmount = Mathf.Clamp01(overshootAmount);

        if (overshootAmount <= 0.0001f)
            return t;

        float s = Mathf.Lerp(0f, 1.5f, overshootAmount);

        float u = t - 1f;
        return 1f + ((s + 1f) * u * u * u) + (s * u * u);
    }

    private static float EaseInOutCos(float t)
    {
        t = Mathf.Clamp01(t);
        return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t);
    }

    private static void SetLocalEulerZ(RectTransform rect, float z)
    {
        rect.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}