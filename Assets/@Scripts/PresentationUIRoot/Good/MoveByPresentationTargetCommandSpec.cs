using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "Move By (XY)",
    Order = -930)]
public sealed class MoveByPresentationTargetCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Delta")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 dest로 스냅")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class MoveByPresentationTargetCommand : CommandBase, IStepScopedCommand
{
    private readonly MoveByPresentationTargetCommandSpec _spec;

    private RectTransform _rect;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    private bool _hasComputedDest;
    private Vector2 _startPos;
    private Vector2 _destPos;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public MoveByPresentationTargetCommand(MoveByPresentationTargetCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _hasComputedDest = false;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;

        ComputeDestIfNeeded();

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

        _hasComputedDest = false;
        ComputeDestIfNeeded();
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

        ComputeDestIfNeeded();
        CommitFinalState();
    }

    private void ComputeDestIfNeeded()
    {
        if (_hasComputedDest || _rect == null)
            return;

        _hasComputedDest = true;
        _startPos = _rect.anchoredPosition;
        _destPos = _startPos + _spec.delta;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _destPos;

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
            nameof(MoveByPresentationTargetCommand));
    }
}