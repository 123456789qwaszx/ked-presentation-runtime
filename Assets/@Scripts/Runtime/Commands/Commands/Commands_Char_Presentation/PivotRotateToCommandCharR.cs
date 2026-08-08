using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Motion", "PivotRotate To", Order = 101)]
public sealed class PivotRotateToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Targets")]
    [Tooltip("회전시킬 피벗. 보통 SwayPivot 축을 사용.")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_SwayPivot;

    [Header("Target Rotation")]
    [Tooltip("목표 Z 각도. relativeToCurrent=false면 절대값, true면 현재 각도에 더할 값입니다.")]
    public float degree = 24f;

    [Header("Mode")]
    [Tooltip("false면 degree를 절대 Z 각도로 사용합니다. true면 현재 Z 각도에 degree를 더합니다.")]
    public bool relativeToCurrent = false;

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
}

public sealed class PivotRotateToCommandCharR : ClaimTweenCommandBase
{
    private readonly PivotRotateToCommandSpecCharR _spec;

    private RectTransform _rect;

    private float _startRotationZ;
    private float _finalRotationZ;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public PivotRotateToCommandCharR(PivotRotateToCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override bool TryResolveTargets(CommandRunScope scope)
    {
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rig?.GetRect(_spec.target);

        return _rect != null;
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);

        _startRotationZ = NormalizeAngle(_rect.localEulerAngles.z);

        _finalRotationZ = _spec.relativeToCurrent
            ? _startRotationZ + _spec.degree
            : ResolveNearestEquivalentAngle(_startRotationZ, _spec.degree);
    }

    /// <summary>예열 → 접근(오버슈트) → 정착의 3구간을 한 진행률 트윈으로 태운다.</summary>
    protected override Tween CreateTween(float duration)
    {
        float deltaToFinal = _finalRotationZ - _startRotationZ;
        float mainDirection = Mathf.Sign(deltaToFinal);

        if (Mathf.Approximately(mainDirection, 0f))
            mainDirection = 1f;

        float anticipationPortion = Mathf.Clamp01(_spec.anticipationPortion);
        float overshootPortion = Mathf.Clamp01(_spec.overshootPortion);

        bool useAnticipation =
            _spec.useAnticipation &&
            !Mathf.Approximately(_spec.anticipation, 0f) &&
            anticipationPortion > 0f;

        bool useOvershoot =
            _spec.useOvershoot &&
            !Mathf.Approximately(_spec.overshoot, 0f) &&
            overshootPortion > 0f &&
            overshootPortion < 1f;

        if (!useAnticipation)
            anticipationPortion = 0f;

        if (!useOvershoot)
            overshootPortion = 1f;

        float anticipationZ = _startRotationZ + (-mainDirection * _spec.anticipation);
        float overshootZ = _finalRotationZ + (mainDirection * _spec.overshoot);

        return DOTween
            .To(
                () => 0f,
                t =>
                {
                    float u = Mathf.Clamp01(t);
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

                    SetLocalEulerZ(_rect, z);
                },
                1f,
                duration)
            .SetEase(Ease.Linear)
            .SetTarget(_rect);
    }

    /// <summary>
    /// 스텝 경계 마무리는 다단 연출을 다시 태우지 않는다 —
    /// 현재 각에서 목표까지 최단 경로로 정착만 시킨다.
    /// </summary>
    protected override Tween CreateAcceleratedTween(float duration)
    {
        float currentZ = NormalizeAngle(_rect.localEulerAngles.z);
        float targetZ = ResolveNearestEquivalentAngle(currentZ, _finalRotationZ);

        return DOTween
            .To(() => currentZ, z => SetLocalEulerZ(_rect, z), targetZ, duration)
            .SetEase(_spec.settleEase)
            .SetTarget(_rect);
    }

    protected override void OnCommitFinalState()
    {
        SetLocalEulerZ(_rect, _finalRotationZ);
    }

    protected override float MeasureRemainingRatio()
    {
        float currentZ = NormalizeAngle(_rect.localEulerAngles.z);
        float targetZ = ResolveNearestEquivalentAngle(currentZ, _finalRotationZ);

        return RemainingRatio(
            Mathf.Abs(_finalRotationZ - _startRotationZ),
            Mathf.Abs(targetZ - currentZ));
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
