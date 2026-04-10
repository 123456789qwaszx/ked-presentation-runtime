using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Rotate To", Order = 101)]
public sealed class SwayRotateToCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Targets")]
    [Tooltip("회전시킬 피벗. 보통 SwayPivot 축을 사용.")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_SwayPivot;

    [Header("Target Rotation")]
    [Tooltip("목표 절대 Z 각도.")]
    public float targetZ = 24f;

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
}

public sealed class SwayRotateToCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SwayRotateToCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    private float _startRotationZ;
    private float _currentRotationZ;
    private float _finalRotationZ;

    private Sequence _sequence;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SwayRotateToCommandCharR(SwayRotateToCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _rect.DOKill(false);
        _sequence?.Kill(false);
        _sequence = null;

        _startRotationZ = NormalizeAngle(_rect.localEulerAngles.z);
        _currentRotationZ = _startRotationZ;
        _finalRotationZ = ResolveNearestEquivalentAngle(_startRotationZ, _spec.targetZ);

        if (_spec.duration <= 0f)
        {
            SetLocalEulerZ(_rect, _finalRotationZ);
            _currentRotationZ = _finalRotationZ;
            yield break;
        }

        float total = Mathf.Max(0.0001f, _spec.duration);

        float deltaToFinal = _finalRotationZ - _startRotationZ;
        float mainDirection = Mathf.Sign(deltaToFinal);
        if (Mathf.Approximately(mainDirection, 0f))
            mainDirection = 1f;

        float aP = Mathf.Clamp01(_spec.anticipationPortion);
        float oP = Mathf.Clamp01(_spec.overshootPortion);

        bool useA = _spec.useAnticipation &&
                    !Mathf.Approximately(_spec.anticipation, 0f) &&
                    aP > 0f;

        bool useO = _spec.useOvershoot &&
                    !Mathf.Approximately(_spec.overshoot, 0f) &&
                    oP > 0f &&
                    oP < 1f;

        if (!useA)
            aP = 0f;

        if (!useO)
            oP = 1f;

        float anticipationZ = _startRotationZ + (-mainDirection * _spec.anticipation);
        float overshootZ = _finalRotationZ + (mainDirection * _spec.overshoot);

        float tA = total * aP;
        float tApproach = total * (useO ? Mathf.Max(0.0001f, oP - aP) : Mathf.Max(0.0001f, 1f - aP));
        float tSettle = useO ? total * Mathf.Max(0.0001f, 1f - oP) : 0f;

        _sequence = DOTween.Sequence().SetUpdate(true);

        if (useA)
        {
            _sequence.Append(DOTween.To(
                    () => _currentRotationZ,
                    z =>
                    {
                        _currentRotationZ = z;
                        SetLocalEulerZ(_rect, z);
                    },
                    anticipationZ,
                    tA)
                .SetEase(_spec.anticipationEase));
        }

        if (useO)
        {
            _sequence.Append(DOTween.To(
                    () => _currentRotationZ,
                    z =>
                    {
                        _currentRotationZ = z;
                        SetLocalEulerZ(_rect, z);
                    },
                    overshootZ,
                    tApproach)
                .SetEase(_spec.approachEase));

            _sequence.Append(DOTween.To(
                    () => _currentRotationZ,
                    z =>
                    {
                        _currentRotationZ = z;
                        SetLocalEulerZ(_rect, z);
                    },
                    _finalRotationZ,
                    tSettle)
                .SetEase(_spec.settleEase));
        }
        else
        {
            _sequence.Append(DOTween.To(
                    () => _currentRotationZ,
                    z =>
                    {
                        _currentRotationZ = z;
                        SetLocalEulerZ(_rect, z);
                    },
                    _finalRotationZ,
                    tApproach)
                .SetEase(_spec.approachEase));
        }

        _sequence.OnComplete(() =>
        {
            if (_rect == null)
                return;

            _currentRotationZ = _finalRotationZ;
            SetLocalEulerZ(_rect, _finalRotationZ);
        });

        if (_spec.wait)
            yield return _sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        OnCommandCompleted(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _sequence?.Kill(false);
        _rect.DOKill(false);

        _startRotationZ = NormalizeAngle(_rect.localEulerAngles.z);
        _finalRotationZ = ResolveNearestEquivalentAngle(_startRotationZ, _spec.targetZ);

        _currentRotationZ = _finalRotationZ;
        SetLocalEulerZ(_rect, _finalRotationZ);

        _sequence = null;
        _rect = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
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

    private static void SetLocalEulerZ(RectTransform rect, float z)
    {
        if (rect == null)
            return;

        rect.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}