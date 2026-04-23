using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Motion", "Move By", Order = -810)]
public sealed class MoveByPresentationCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationTarget target = PresentationTarget.StagePan_Root;

    [Header("Move")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;

    [Header("Wait")]
    public bool wait = false;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class MoveByPresentationCommand : CommandBase
{
    private readonly MoveByPresentationCommandSpec _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _destPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public MoveByPresentationCommand(MoveByPresentationCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;

        if (_spec.duration <= 0f || _spec.delta == Vector2.zero)
        {
            _rect.anchoredPosition = _destPos;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector2 dest = _destPos;
        Vector2 start = _rect.anchoredPosition;

        _tween = _rect
            .DOAnchorPos(dest, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.anchoredPosition = dest;
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        _rect.anchoredPosition = _destPos;
        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _rect.anchoredPosition = _destPos;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rect = scope.Presentation.GetRect(_spec.target);
        _destPos = _rect.anchoredPosition + _spec.delta;
    }
}