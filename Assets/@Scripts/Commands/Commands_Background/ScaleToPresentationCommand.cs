using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Motion", "Scale To", Order = -809)]
public sealed class ScaleToPresentationCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationTarget target = PresentationTarget.StageZoom_Root;

    [Header("Scale")]
    public Vector2 toScale = Vector2.one;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 스케일 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ScaleToPresentationCommand : CommandBase
{
    private readonly ScaleToPresentationCommandSpec _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector3 _destScale;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ScaleToPresentationCommand(ScaleToPresentationCommandSpec spec)
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

        if (_spec.duration <= 0f || (Vector2)_rect.localScale == _spec.toScale)
        {
            _rect.localScale = _destScale;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector3 dest = _destScale;

        _tween = _rect
            .DOScale(dest, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.localScale = dest;
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

        _rect.localScale = _destScale;
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
        _rect.localScale = _destScale;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rect = scope.Presentation.GetRect(_spec.target);
        _destScale = new Vector3(_spec.toScale.x, _spec.toScale.y, 1f);
    }
}