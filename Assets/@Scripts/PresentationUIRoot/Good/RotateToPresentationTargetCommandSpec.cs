using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "Rotate To",
    Order = -910)]
public sealed class RotateToPresentationTargetCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Rotation (localEulerAngles)")]
    public Vector3 toEuler = Vector3.zero;

    [Header("From")]
    public bool overrideFromEuler = false;
    public Vector3 fromEuler = Vector3.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class RotateToPresentationTargetCommand : CommandBase, IStepScopedCommand
{
    private readonly RotateToPresentationTargetCommandSpec _spec;

    private RectTransform _rect;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public RotateToPresentationTargetCommand(RotateToPresentationTargetCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;

        if (_spec.overrideFromEuler)
            _rect.localEulerAngles = _spec.fromEuler;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOLocalRotate(_spec.toEuler, _spec.duration, RotateMode.Fast)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                CommitFinalState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        CommitFinalState();
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.localEulerAngles = _spec.toEuler;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rect = PresentationTargetResolver.ResolveRect(
            scope,
            _spec.target,
            _spec.strict,
            nameof(RotateToPresentationTargetCommand));
    }
}