using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Emoji",
    "Spring Appear",
    Order = -700)]
public sealed class SpringAppearCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Targets")]
    public CharacterRigTarget scaleTarget = CharacterRigTarget.EmojiSlot00_Scale;
    public CharacterRigTarget effectTarget = CharacterRigTarget.CharacterEmojiSlot00_Effect;
    public CharacterRigTarget rotationTarget = CharacterRigTarget.EmojiSlot00_Rotation;

    [Header("Scale")]
    public Vector2 fromScale = new(0.001f, 0.001f);

    [Tooltip("최종 1.0을 아주 살짝 넘는 정도. 0.03~0.06 권장.")]
    public float overshootAmount = 0.04f;

    [Header("Motion")]
    public Vector2 liftOffset = new(0.75f, 5.5f);

    [Tooltip("등장 중 아주 살짝 기울어지는 각도. 과하면 이모지 존재감이 강해짐.")]
    public float tiltDegrees = -2.8f;

    [Header("Tween")]
    public float duration = 0.46f;
    public Ease ease = Ease.Linear;
}

public sealed class SpringAppearCommandCharR : CommandBase
{
    private readonly SpringAppearCommandSpecCharR _spec;

    private RectTransform _scaleRect;
    private RectTransform _effectRect;
    private RectTransform _rotationRect;

    private Vector2 _baseEffectPos;
    private Quaternion _baseRotation;

    private Tween _tween;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public SpringAppearCommandCharR(SpringAppearCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                ApplyProgress,
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_scaleRect)
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

        KillTween();
        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        _scaleRect = rigRefs.GetRect(_spec.scaleTarget);
        _effectRect = rigRefs.GetRect(_spec.effectTarget);
        _rotationRect = rigRefs.GetRect(_spec.rotationTarget);
    }

    private void ClaimTarget()
    {
        _scaleRect.DOKill(true);
        _effectRect.DOKill(true);
        _rotationRect.DOKill(true);

        _baseEffectPos = _effectRect.anchoredPosition;
        _baseRotation = _rotationRect.localRotation;

        _scaleRect.localScale = new Vector3(_spec.fromScale.x, _spec.fromScale.y, 1f);
        _effectRect.anchoredPosition = _baseEffectPos;
        _rotationRect.localRotation = _baseRotation;

        HasClaimedTarget = true;
    }

    private void ApplyProgress(float u)
    {
        u = Mathf.Clamp01(u);

        // 빠르게 피어나되, 분리된 squash/rebound처럼 보이지 않도록 하나의 부드러운 곡선으로 처리.
        float bloom = NormalizedExpoOut(u, 7.5f);

        // 최종적으로는 0이 되는 아주 작은 overshoot.
        // 한 번의 호흡 안에서만 살짝 부풀었다가 사라진다.
        float overshootEnvelope =
            Mathf.Sin(Mathf.PI * u) *
            Mathf.Pow(1f - u, 0.85f);

        float scaleT = bloom + (_spec.overshootAmount * overshootEnvelope);

        Vector3 scale = Vector3.LerpUnclamped(
            new Vector3(_spec.fromScale.x, _spec.fromScale.y, 1f),
            Vector3.one,
            scaleT);

        _scaleRect.localScale = scale;

        // 크기가 생기는 힘 때문에 아주 살짝 위로 뜨지만,
        // hop처럼 보이지 않도록 한 번의 낮은 hump만 만든다.
        float liftEnvelope =
            Mathf.Sin(Mathf.PI * u) *
            (1f - 0.25f * u);

        _effectRect.anchoredPosition =
            _baseEffectPos + (_spec.liftOffset * liftEnvelope);

        // 기울어짐도 한 번의 호흡 안에서만 생겼다가 자연스럽게 0으로 사라진다.
        float tiltEnvelope =
            Mathf.Sin(Mathf.PI * u) *
            Mathf.Pow(1f - u, 0.65f);

        float z = _spec.tiltDegrees * tiltEnvelope;

        _rotationRect.localRotation =
            _baseRotation * Quaternion.Euler(0f, 0f, z);
    }

    private void CommitFinalState()
    {
        KillTween();

        _scaleRect.localScale = Vector3.one;
        _effectRect.anchoredPosition = _baseEffectPos;
        _rotationRect.localRotation = _baseRotation;

        HasClaimedTarget = false;
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        _tween = null;
    }

    private static float NormalizedExpoOut(float u, float sharpness)
    {
        if (u <= 0f)
            return 0f;

        if (u >= 1f)
            return 1f;

        float numerator = 1f - Mathf.Exp(-sharpness * u);
        float denominator = 1f - Mathf.Exp(-sharpness);

        return numerator / denominator;
    }
}