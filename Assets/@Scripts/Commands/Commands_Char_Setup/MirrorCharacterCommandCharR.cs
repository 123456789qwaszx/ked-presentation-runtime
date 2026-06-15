using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum CharacterMirrorMode
{
    Toggle,
    Left,
    Right,
}

[Serializable]
[CommandMenuHint(
    "Char Rig Composition",
    "Mirror Character",
    Order = -198)]
public sealed class MirrorCharacterCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Mirror")]
    public CharacterMirrorMode mode = CharacterMirrorMode.Toggle;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_ActingScale_X;

    [Header("Tween")]
    public float duration = 0f;
    public Ease ease = Ease.OutCubic;
}

public sealed class MirrorCharacterCommandCharR : CommandBase
{
    private readonly MirrorCharacterCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector3 _baseScale;
    private Vector3 _targetScale;

    private Tween _tween;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public MirrorCharacterCommandCharR(MirrorCharacterCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefsAndState(scope);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOScale(_targetScale, _spec.duration)
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
            ResolveRefsAndState(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefsAndState(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string resolvedRigKey =
            CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, _spec.slotKey);

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        _rect = rigRefs.GetRect(_spec.target);

        CharacterFacing facing = ResolveTargetFacing(scope, resolvedRigKey);

        scope.CastRegistry.SetFacing(resolvedRigKey, facing);

        _baseScale = _rect.localScale;
        _targetScale = _baseScale;
        _targetScale.x = Mathf.Abs(_baseScale.x) * facing.Sign();
    }

    private CharacterFacing ResolveTargetFacing(
        CommandRunScope scope,
        string resolvedRigKey)
    {
        switch (_spec.mode)
        {
            case CharacterMirrorMode.Left:
                return CharacterFacing.Left;

            case CharacterMirrorMode.Right:
                return CharacterFacing.Right;

            case CharacterMirrorMode.Toggle:
            default:
            {
                if (scope.CastRegistry.TryGetFacing(resolvedRigKey, out CharacterFacing current))
                    return current.Opposite();

                return CharacterFacing.Left;
            }
        }
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);
        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        KillTween();

        if (_rect != null)
            _rect.localScale = _targetScale;

        HasClaimedTarget = false;
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        CommitFinalState();
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        _tween = null;
    }
}