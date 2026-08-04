
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Emoji Rotate To", Order = -694)]
public sealed class EmojiRotateToCommandSpecCharR : CharacterRigCommandSpecBase
{
    public string emojiKey;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Rotation;
    public Vector3 toEuler = Vector3.zero;
    public bool relativeToCurrent = false;
    public bool overrideFromEuler = false;
    public Vector3 fromEuler = Vector3.zero;
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class EmojiRotateToCommandCharR : CharacterEmojiCommandBase
{
    private readonly EmojiRotateToCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _rect;
    private Vector3 _targetEuler;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiRotateToCommandCharR(
        EmojiRotateToCommandSpecCharR spec,
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
            .DOLocalRotate(_targetEuler, _spec.duration, RotateMode.Fast)
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

        if (_spec.overrideFromEuler)
            _rect.localEulerAngles = context.MirrorEulerZ(_spec.fromEuler);

        Vector3 startEuler = _rect.localEulerAngles;
        Vector3 effectiveToEuler = context.MirrorEulerZ(_spec.toEuler);
        _targetEuler = _spec.relativeToCurrent
            ? startEuler + effectiveToEuler
            : effectiveToEuler;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.localEulerAngles = _targetEuler;

        HasClaimedTarget = false;
        _tween = null;
    }
}