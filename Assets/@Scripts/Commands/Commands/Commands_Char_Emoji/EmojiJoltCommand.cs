
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Emoji Jolt", Order = -691)]
public sealed class EmojiJoltCommandSpec : CharacterRigCommandSpecBase
{
    public string emojiKey;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Track_Move;
    public float strength = 22f;
    public CharRigDirection direction = CharRigDirection.Right;
    public float duration = 0.88f;
    [Min(1)] public int taps = 3;
    public float damping = 6f;
    public float anticipation = 3f;
}

public sealed class EmojiJoltCommand : CharacterEmojiCommandBase
{
    private readonly EmojiJoltCommandSpec _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _rect;
    private Vector2 _basePos;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiJoltCommand(
        EmojiJoltCommandSpec spec,
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

        ClaimTarget();

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            CommitFinalState();
            yield break;
        }

        float amplitude = Mathf.Abs(_spec.strength);
        int taps = Mathf.Max(1, _spec.taps);
        float damping = Mathf.Max(0.01f, _spec.damping);
        float anticipation = Mathf.Abs(_spec.anticipation);
        Vector2 dir = GetSignedDirection(context.MirrorDirection(_spec.direction));

        _tween = DOTween
            .To(
                () => 0f,
                u =>
                {
                    u = Mathf.Clamp01(u);
                    float antiTerm = 0f;
                    if (!Mathf.Approximately(anticipation, 0f))
                    {
                        float s = Mathf.Clamp01(u / 0.15f);
                        float bump = Mathf.Sin(Mathf.PI * s);
                        antiTerm = -anticipation * bump * (1f - s);
                    }

                    float decay = Mathf.Exp(-damping * u);
                    float settleEnvelope = Mathf.Sin(Mathf.PI * u);
                    float osc = Mathf.Sin(2f * Mathf.PI * taps * u);
                    float scalar = antiTerm + (amplitude * decay * osc * settleEnvelope);
                    _rect.anchoredPosition = _basePos + dir * scalar;
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
            ClaimTarget();

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

    private void ClaimTarget()
    {
        _rect.DOKill(true);
        _basePos = _rect.anchoredPosition;
        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _basePos;

        HasClaimedTarget = false;
        _tween = null;
    }
}