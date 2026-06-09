using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Background Rig Motion", "Breathe In Place", Order = -753)]
public sealed class BreathInPlaceCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Track;

    [Header("Timing")]
    [Tooltip("전체 숨쉬기 지속 시간.")]
    public float duration = 99.0f;

    [Tooltip("초당 호흡 횟수. 0.2면 약 5초에 한 번 천천히 오르내립니다.")]
    public float breathsPerSecond = 0.2f;

    [Header("Motion")]
    [Tooltip("위아래 움직임 높이. 픽셀 단위. 배경에는 작게 쓰는 것을 권장합니다.")]
    public float height = 6f;

    [Tooltip("좌우 흔들림. 배경에는 0~2 정도만 권장합니다.")]
    public float sideSway = 0f;

    [Header("Scale Pulse")]
    [Tooltip("체크하면 위치뿐 아니라 localScale도 아주 살짝 숨쉬듯 변화합니다.")]
    public bool useScalePulse = false;

    [Tooltip("숨쉴 때 추가 scale 양. 0.005면 최대 약 0.5% 커집니다.")]
    public float scaleAmount = 0.005f;

    [Header("Feel")]
    public Ease ease = Ease.InOutSine;

    [Tooltip("시작 위상. 여러 배경/레이어를 동시에 움직일 때 살짝 다르게 줄 수 있습니다.")]
    public float phaseOffset = 0f;

    [Header("Blend")]
    public float blendIn = 0.25f;
    public float blendOut = 0.25f;
}

public sealed class BreathInPlaceCommandBgR : CommandBase
{
    private readonly BreathInPlaceCommandSpecBgR _spec;

    private RectTransform _rect;

    private Vector2 _basePos;
    private Vector3 _baseScale;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public BreathInPlaceCommandBgR(BreathInPlaceCommandSpecBgR spec)
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

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rig =
            BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);

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
}