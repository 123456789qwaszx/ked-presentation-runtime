
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Emoji Slide In", Order = -689)]
public sealed class EmojiSlideInCommandSpecCharR : CharacterRigCommandSpecBase
{
    public string emojiKey;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Track_Move;
    public CharRigDirection direction = CharRigDirection.Left;
    public float distance = 480f;
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;
    public float punch = 24f;
}

public sealed class EmojiSlideInCommandCharR : CharacterEmojiCommandBase
{
    private readonly EmojiSlideInCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _rect;
    private Vector2 _startPos;
    private Vector2 _destPos;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiSlideInCommandCharR(
        EmojiSlideInCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        CharacterEmojiMirrorContext context = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            _spec.emojiKey);

        ClaimTarget(context);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 start = _startPos;
        Vector2 dest = _destPos;
        Vector2 fromDir = GetSignedDirection(context.MirrorDirection(_spec.direction));
        Vector2 slideDir = dest - start;
        slideDir = slideDir.sqrMagnitude > 0f ? slideDir.normalized : -fromDir;

        _rect.anchoredPosition = start;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
                    Vector2 basePos = Vector2.LerpUnclamped(start, dest, e);
                    float bump = Mathf.Sin(Mathf.PI * e) * Mathf.Pow(1f - e, 0.65f);
                    Vector2 offset = slideDir * (_spec.punch * bump);
                    _rect.anchoredPosition = basePos + offset;
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
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
        {
            CharacterEmojiMirrorContext context = ResolveEmojiMirrorContext(
                scope,
                _resolver,
                _spec.slotKey,
                _spec.emojiKey);
            ClaimTarget(context);
        }

        CommitFinalState();
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween?.Kill(false);
        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rigRefs.GetRect(_spec.target);
    }

    private void ClaimTarget(CharacterEmojiMirrorContext context)
    {
        _rect.DOKill(true);
        _destPos = _rect.anchoredPosition;
        _startPos = _destPos + GetSignedDirection(context.MirrorDirection(_spec.direction)) * _spec.distance;
        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _destPos;

        HasClaimedTarget = false;
        _tween = null;
    }
}
