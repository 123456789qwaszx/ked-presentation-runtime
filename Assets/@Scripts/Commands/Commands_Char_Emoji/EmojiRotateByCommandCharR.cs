
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Emoji Rotate By", Order = -693)]
public sealed class EmojiRotateByCommandSpecCharR : CharacterRigCommandSpecBase
{
    public string emojiKey;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Rotation;
    public Vector3 deltaEuler = Vector3.zero;
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class EmojiRotateByCommandCharR : CharacterEmojiCommandBase
{
    private readonly EmojiRotateByCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _rect;
    private Vector3 _startEuler;
    private Vector3 _destEuler;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiRotateByCommandCharR(
        EmojiRotateByCommandSpecCharR spec,
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

        _tween = DOTween
            .To(
                () => 0f,
                ApplyProgress,
                1f,
                _spec.duration)
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
        CommitFinalState();
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

        _startEuler = _rect.localEulerAngles;
        _destEuler = _startEuler + context.MirrorEulerZ(_spec.deltaEuler);
        HasClaimedTarget = true;
    }

    private void ApplyProgress(float progress)
    {
        _rect.localEulerAngles = Vector3.LerpUnclamped(_startEuler, _destEuler, Mathf.Clamp01(progress));
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.localEulerAngles = _destEuler;

        HasClaimedTarget = false;
        _tween = null;
    }
}
