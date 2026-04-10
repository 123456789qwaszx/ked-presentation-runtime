using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Rotate To", Order = 101)]
public sealed class SwayRotateToCommandSpecCharR : CommandSpecBase
{
    [Header("Targets")]
    [Tooltip("회전시킬 피벗. 보통 SwayPivot 축을 사용.")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_SwayPivot;

    [Header("Target Rotation")]
    [Tooltip("목표 절대 Z 각도.")]
    public float degree = 24f;

    [Header("Anticipation")]
    [Tooltip("시작 전에 반대 방향으로 살짝 당기는 양(도). 0이면 비활성.")]
    public bool useAnticipation = true;

    [Tooltip("당기는 양. 양수면 메인 진행 반대 방향, 음수면 같은 방향.")]
    public float anticipation = 0.4f;

    [Range(0f, 1f)]
    [Tooltip("전체 시간 중 anticipation이 차지하는 비율.")]
    public float anticipationPortion = 0.06f;

    public Ease anticipationEase = Ease.OutQuad;

    [Header("Overshoot")]
    [Tooltip("목표를 살짝 지나쳤다가 돌아오는 양(도). 0이면 비활성.")]
    public bool useOvershoot = true;

    [Tooltip("지나치는 양. 양수면 목표를 넘고, 음수면 덜 가는 느낌.")]
    public float overshoot = 0.4f;

    [Range(0f, 1f)]
    [Tooltip("전체 시간 중 overshoot 지점에 도달하는 시점 비율.")]
    public float overshootPortion = 0.85f;

    public Ease approachEase = Ease.OutCubic;
    public Ease settleEase = Ease.OutQuart;

    [Header("Timing")]
    [Tooltip("총 길이. <= 0이면 즉시 적용.")]
    public float duration = 0.7f;

    [Header("Wait")]
    public bool wait = false;
    
    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class SwayRotateToCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SwayRotateToCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    private float _startRotationZ;
    private float _currentRotationZ;
    private float _finalRotationZ;

    private Tween _tween;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SwayRotateToCommandCharR(SwayRotateToCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.
        
        _tween?.Kill(false);
        _tween = null;
        _canCommitFinalState = true;

        _startRotationZ = NormalizeAngle(_rect.localEulerAngles.z);
        _currentRotationZ = _startRotationZ;
        _finalRotationZ = ResolveNearestEquivalentAngle(_startRotationZ, _spec.degree);

        if (_spec.duration <= 0f)
        {
            SetLocalEulerZ(_rect, _finalRotationZ);
            _currentRotationZ = _finalRotationZ;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        float total = Mathf.Max(0.0001f, _spec.duration);

        float deltaToFinal = _finalRotationZ - _startRotationZ;
        float mainDirection = Mathf.Sign(deltaToFinal);
        if (Mathf.Approximately(mainDirection, 0f))
            mainDirection = 1f;

        float anticipationPortion = Mathf.Clamp01(_spec.anticipationPortion);
        float overshootPortion = Mathf.Clamp01(_spec.overshootPortion);

        bool useAnticipation = _spec.useAnticipation &&
                               !Mathf.Approximately(_spec.anticipation, 0f) &&
                               anticipationPortion > 0f;

        bool useOvershoot = _spec.useOvershoot &&
                            !Mathf.Approximately(_spec.overshoot, 0f) &&
                            overshootPortion > 0f &&
                            overshootPortion < 1f;

        if (!useAnticipation)
            anticipationPortion = 0f;

        if (!useOvershoot)
            overshootPortion = 1f;

        float anticipationZ = _startRotationZ + (-mainDirection * _spec.anticipation);
        float overshootZ = _finalRotationZ + (mainDirection * _spec.overshoot);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float u = Mathf.Clamp01(t / total);
                    float z;

                    if (useAnticipation && u < anticipationPortion)
                    {
                        float localU = SafeInverseLerp(0f, anticipationPortion, u);
                        float eased = DOVirtual.EasedValue(0f, 1f, localU, _spec.anticipationEase);
                        z = Mathf.LerpUnclamped(_startRotationZ, anticipationZ, eased);
                    }
                    else if (useOvershoot && u < overshootPortion)
                    {
                        float startU = useAnticipation ? anticipationPortion : 0f;
                        float fromZ = useAnticipation ? anticipationZ : _startRotationZ;

                        float localU = SafeInverseLerp(startU, overshootPortion, u);
                        float eased = DOVirtual.EasedValue(0f, 1f, localU, _spec.approachEase);
                        z = Mathf.LerpUnclamped(fromZ, overshootZ, eased);
                    }
                    else
                    {
                        float startU = useOvershoot ? overshootPortion : (useAnticipation ? anticipationPortion : 0f);
                        float fromZ = useOvershoot ? overshootZ : (useAnticipation ? anticipationZ : _startRotationZ);
                        Ease ease = useOvershoot ? _spec.settleEase : _spec.approachEase;

                        float localU = SafeInverseLerp(startU, 1f, u);
                        float eased = DOVirtual.EasedValue(0f, 1f, localU, ease);
                        z = Mathf.LerpUnclamped(fromZ, _finalRotationZ, eased);
                    }

                    _currentRotationZ = z;
                    SetLocalEulerZ(_rect, z);
                },
                total,
                total
            )
            .SetEase(Ease.Linear)
            .SetTarget(_rect)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState)
                    return;

                _currentRotationZ = _finalRotationZ;
                SetLocalEulerZ(_rect, _finalRotationZ);
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);

        _startRotationZ = NormalizeAngle(_rect.localEulerAngles.z);
        _finalRotationZ = ResolveNearestEquivalentAngle(_startRotationZ, _spec.degree);

        _currentRotationZ = _finalRotationZ;
        SetLocalEulerZ(_rect, _finalRotationZ);

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig))
            return;

        _rect = rig.GetRect(_spec.target);
    }

    private static float ResolveNearestEquivalentAngle(float reference, float target)
    {
        target = NormalizeAngle(target);

        while (target - reference > 180f)
            target -= 360f;

        while (target - reference < -180f)
            target += 360f;

        return target;
    }

    private static float SafeInverseLerp(float a, float b, float value)
    {
        float d = b - a;
        if (Mathf.Abs(d) <= 0.0001f)
            return 1f;

        return Mathf.Clamp01((value - a) / d);
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