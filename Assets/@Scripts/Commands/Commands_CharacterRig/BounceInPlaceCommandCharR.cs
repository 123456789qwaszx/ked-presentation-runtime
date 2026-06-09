using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Bounce In Place", Order = -754)]
public sealed class BounceInPlaceCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Track;

    [Header("Timing")]
    [Tooltip("전체 bounce 지속 시간.")]
    public float duration = 99f;

    [Tooltip("초당 bounce 횟수. 2.5면 1초에 약 2.5번 튑니다.")]
    public float bouncesPerSecond = 2.5f;

    [Header("Motion")]
    [Tooltip("위로 튀어오르는 높이. 픽셀 단위.")]
    public float height = 32f;

    [Range(0.05f, 0.8f)]
    [Tooltip("한 bounce 안에서 상승에 쓰는 시간 비율. 작을수록 빠르게 탁 튀어오릅니다.")]
    public float riseRatio = 0.18f;

    [Tooltip("좌우 흔들림. 0이면 위아래로만 움직입니다.")]
    public float sideSway = 0f;

    [Header("Feel")]
    [Tooltip("상승 커브. 값이 클수록 초반에 더 빠르게 튀어오릅니다.")]
    public Ease riseEase = Ease.OutQuad;

    [Tooltip("하강 커브. 값이 클수록 천천히 내려오다가 마지막에 닿는 느낌이 납니다.")]
    public Ease fallEase = Ease.InQuad;

    [Header("Blend")]
    [Tooltip("시작할 때 자연스럽게 motion이 켜지는 시간.")]
    public float blendIn = 0.04f;

    [Tooltip("끝날 때 자연스럽게 원래 위치로 돌아오는 시간.")]
    public float blendOut = 0.08f;
}

public sealed class BounceInPlaceCommandCharR : CommandBase
{
    private readonly BounceInPlaceCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _basePos;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public BounceInPlaceCommandCharR(BounceInPlaceCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f || _spec.bouncesPerSecond <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = DOTween
            .To(
                () => 0f,
                elapsed =>
                {
                    float phase = Mathf.Repeat(elapsed * _spec.bouncesPerSecond, 1f);
                    float envelope = EvaluateEnvelope(elapsed, _spec.duration, _spec.blendIn, _spec.blendOut);

                    float y = EvaluateBounceHeight(
                        phase,
                        _spec.height,
                        _spec.riseRatio,
                        _spec.riseEase,
                        _spec.fallEase);

                    float x = Mathf.Sin(phase * Mathf.PI * 2f) * _spec.sideSway;

                    Vector2 offset = new Vector2(x, y) * envelope;
                    _rect.anchoredPosition = _basePos + offset;
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

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _basePos;

        HasClaimedTarget = false;
    }

    private static float EvaluateBounceHeight(
        float phase,
        float height,
        float riseRatio,
        Ease riseEase,
        Ease fallEase)
    {
        phase = Mathf.Clamp01(phase);
        riseRatio = Mathf.Clamp(riseRatio, 0.05f, 0.8f);

        if (height == 0f)
            return 0f;

        if (phase <= riseRatio)
        {
            float u = phase / riseRatio;
            float e = DOVirtual.EasedValue(0f, 1f, u, riseEase);
            return Mathf.LerpUnclamped(0f, height, e);
        }

        {
            float u = (phase - riseRatio) / (1f - riseRatio);
            float e = DOVirtual.EasedValue(0f, 1f, u, fallEase);
            return Mathf.LerpUnclamped(height, 0f, e);
        }
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