using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "Move To (XY)",
    Order = -929)]
public sealed class MoveToPresentationTargetCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Position")]
    public Vector2 to = Vector2.zero;

    [Header("From")]
    public bool overrideFrom = false;
    public Vector2 from = Vector2.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class MoveToPresentationTargetCommand : CommandBase, IStepScopedCommand
{
    private readonly MoveToPresentationTargetCommandSpec _spec;

    private RectTransform _rect;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public MoveToPresentationTargetCommand(MoveToPresentationTargetCommandSpec spec)
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

        if (_spec.overrideFrom)
            _rect.anchoredPosition = _spec.from;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOAnchorPos(_spec.to, _spec.duration)
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
            _rect.anchoredPosition = _spec.to;

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
            nameof(MoveToPresentationTargetCommand));
    }
}