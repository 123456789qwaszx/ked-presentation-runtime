using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Emoji Move By", Order = -695)]
public sealed class EmojiMoveByCommandSpecCharR : CharacterRigCommandSpecBase
{
    public string emojiKey;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Effect;
    public bool useAbsolutePosition = false;
    public Vector2 delta = Vector2.zero;
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class EmojiMoveByCommandCharR : CharacterEmojiCommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly EmojiMoveByCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _rect;
    private Vector2 _startPos;
    private Vector2 _destPos;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiMoveByCommandCharR(
        EmojiMoveByCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOAnchorPos(_destPos, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget(scope);

        CommitFinalState();
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween?.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();
        if (duration <= 0f)
        {
            CommitFinalState();
            return;
        }

        _tween = _rect
            .DOAnchorPos(_destPos, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rigRefs.GetRect(_spec.target);
    }

    private void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);

        CharacterEmojiMirrorContext context = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            _spec.emojiKey);

        Vector2 effectiveDelta = context.MirrorMotionVector(_spec.delta);

        _startPos = _rect.anchoredPosition;
        _destPos = _spec.useAbsolutePosition
            ? effectiveDelta
            : _startPos + effectiveDelta;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _destPos;

        HasClaimedTarget = false;
        _tween = null;
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = Vector2.Distance(_startPos, _destPos);
        float remainingDistance = Vector2.Distance(_rect.anchoredPosition, _destPos);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }
}


[Serializable]
[CommandMenuHint("Char Rig Emoji", "Emoji Tremble", Order = -690)]
public sealed class EmojiTrembleCommandSpecCharR : CharacterRigCommandSpecBase
{
    public string emojiKey;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Track_Move;
    public float strength = 8f;
    public CharRigDirection direction = CharRigDirection.Right;
    public float duration = 1.2f;
    public float frequency = 24f;
    [Range(0f, 1f)] public float crossAxisRatio = 0.35f;
    [Range(0f, 1f)] public float noiseRatio = 0.25f;
    public bool usePulse = false;
    public float pulseInterval = 1.0f;
    public float pulseDuration = 0.16f;
    public float blendIn = 0.04f;
    public float blendOut = 0.08f;
}