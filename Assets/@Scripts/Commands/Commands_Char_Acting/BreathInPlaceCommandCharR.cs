using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Breathe In Place", Order = -753)]
public sealed class BreathInPlaceCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Track_Idle;

    [Header("Timing")]
    [Tooltip("전체 숨쉬기 지속 시간.")]
    public float duration = 99.0f;

    [Tooltip("초당 호흡 횟수. 0.35면 약 2.8초에 한 번 천천히 오르내립니다.")]
    public float breathsPerSecond = 0.3f;

    [Header("Motion")]
    [Tooltip("위아래 움직임 높이. 픽셀 단위. 작을수록 숨쉬는 듯한 미세 움직임.")]
    public float height = 10f;

    [Tooltip("좌우 흔들림. 보통 숨쉬기에는 0~1 정도만 권장합니다.")]
    public float sideSway = 0f;

    [Header("Scale Pulse")]
    [Tooltip("체크하면 위치뿐 아니라 localScale도 아주 살짝 숨쉬듯 변화합니다.")]
    public bool useScalePulse = false;

    [Tooltip("숨쉴 때 추가 scale 양. 0.015면 최대 약 1.5% 커집니다.")]
    public float scaleAmount = 0.015f;

    [Header("Feel")]
    [Tooltip("위치 움직임 커브. Sine 기반이라 기본적으로 매우 부드럽습니다.")]
    public Ease ease = Ease.InOutSine;

    [Tooltip("호흡 움직임의 시작 위상. 여러 캐릭터를 동시에 움직일 때 살짝 다르게 주면 덜 기계적입니다.")]
    public float phaseOffset = 0f;

    [Header("Blend")]
    [Tooltip("시작할 때 자연스럽게 motion이 켜지는 시간.")]
    public float blendIn = 0.25f;

    [Tooltip("끝날 때 자연스럽게 원래 위치로 돌아오는 시간.")]
    public float blendOut = 0.25f;
}

public sealed class BreathInPlaceCommandCharR : CommandBase, IRunScopedCommand
{
    private readonly BreathInPlaceCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector2 _basePos;
    private Vector3 _baseScale;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public BreathInPlaceCommandCharR(BreathInPlaceCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f || _spec.breathsPerSecond <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        float duration = Mathf.Max(0.01f, _spec.duration);
        float breathsPerSecond = Mathf.Max(0.01f, _spec.breathsPerSecond);
        float height = _spec.height;
        float sideSway = _spec.sideSway;
        float scaleAmount = Mathf.Max(0f, _spec.scaleAmount);

        Tween tween = DOTween
            .To(
                () => 0f,
                elapsed =>
                {
                    float envelope = EvaluateEnvelope(
                        elapsed,
                        duration,
                        _spec.blendIn,
                        _spec.blendOut);

                    float phase = (elapsed * breathsPerSecond + _spec.phaseOffset) * Mathf.PI * 2f;

                    float breath01 = (Mathf.Sin(phase - Mathf.PI * 0.5f) + 1f) * 0.5f;
                    float eased = DOVirtual.EasedValue(0f, 1f, breath01, _spec.ease);

                    float y = eased * height;
                    float x = Mathf.Sin(phase) * sideSway;

                    Vector2 offset = new Vector2(x, y) * envelope;
                    _rect.anchoredPosition = _basePos + offset;

                    if (_spec.useScalePulse)
                    {
                        float scalePulse = 1f + eased * scaleAmount * envelope;
                        _rect.localScale = new Vector3(
                            _baseScale.x * scalePulse,
                            _baseScale.y * scalePulse,
                            _baseScale.z);
                    }
                },
                duration,
                duration)
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

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);

        _basePos = _rect.anchoredPosition;
        _baseScale = _rect.localScale;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _basePos;
        _rect.localScale = _baseScale;

        HasClaimedTarget = false;
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
}