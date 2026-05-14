using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "Slide In",
    Order = -900)]
public sealed class SlideInPresentationTargetCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Slide")]
    public PresentationDirection direction = PresentationDirection.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("Overshoot")]
    [Tooltip("0이면 일반 SlideIn에 가까워집니다.")]
    public float punch = 24f;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class SlideInPresentationTargetCommand : CommandBase, IStepScopedCommand
{
    private readonly SlideInPresentationTargetCommandSpec _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _destPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlideInPresentationTargetCommand(SlideInPresentationTargetCommandSpec spec)
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

        if (scope.IsRollbackSeeking)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 dest = _destPos;
        Vector2 fromDir = GetDir(_spec.direction);
        Vector2 start = dest + fromDir * _spec.distance;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 slideDir = dest - start;
        slideDir = slideDir.sqrMagnitude > 0f
            ? slideDir.normalized
            : -fromDir;

        _rect.anchoredPosition = start;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    Vector2 basePos = Vector2.LerpUnclamped(start, dest, e);
                    float bump = JuicyBumpEnd(e);
                    Vector2 offset = slideDir * (_spec.punch * bump);

                    _rect.anchoredPosition = basePos + offset;
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
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
            nameof(SlideInPresentationTargetCommand));

        if (_rect != null)
            _destPos = _rect.anchoredPosition;
    }

    private static Vector2 GetDir(PresentationDirection from)
    {
        switch (from)
        {
            case PresentationDirection.Right:
                return new Vector2(+1f, 0f);

            case PresentationDirection.Up:
                return new Vector2(0f, +1f);

            case PresentationDirection.Down:
                return new Vector2(0f, -1f);

            default:
                return new Vector2(-1f, 0f);
        }
    }

    private static float JuicyBumpEnd(float e)
    {
        e = Mathf.Clamp01(e);
        return Mathf.Sin(Mathf.PI * e) * (e * e);
    }
}