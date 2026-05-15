using System;
using DG.Tweening;
using UnityEngine;
using System.Collections;

[Serializable]
[CommandMenuHint("Presentation Motion", "Fade To", Order = -820)]
public sealed class FadeToPresentationCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationTarget target = PresentationTarget.Stage00BGOverlay_Root;

    [Header("Fade")]
    [Range(0f, 1f)]
    public float targetAlpha = 1f;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.35f;

    public Ease ease = Ease.OutCubic;

    [Header("Interaction")]
    [Tooltip("true면 완료 시 interactable을 targetAlpha 기준으로 설정합니다.")]
    public bool controlInteractable = false;

    [Tooltip("true면 완료 시 blocksRaycasts를 targetAlpha 기준으로 설정합니다.")]
    public bool controlBlocksRaycasts = false;

    [Header("Options")]
    [Tooltip("체크하면 기존 CanvasGroup 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}


public sealed class FadeToPresentationCommand : CommandBase
{
    private readonly FadeToPresentationCommandSpec _spec;

    private RectTransform _rect;
    private CanvasGroup _group;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeToPresentationCommand(FadeToPresentationCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null || _group == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _group.DOKill(true);

        _canCommitFinalState = true;

        if (_spec.duration <= 0f)
        {
            ApplyFinalState(_spec.targetAlpha);
            _canCommitFinalState = false;
            _rect = null;
            _group = null;
            _tween = null;
            yield break;
        }

        _tween = DOTween
            .To(
                () => _group != null ? _group.alpha : 0f,
                x =>
                {
                    if (!_canCommitFinalState || _group == null)
                        return;

                    _group.alpha = x;
                },
                _spec.targetAlpha,
                _spec.duration
            )
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_group)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _group == null)
                    return;

                ApplyFinalState(_spec.targetAlpha);

                _canCommitFinalState = false;
                _rect = null;
                _group = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null || _group == null)
            return;

        ApplyFinalState(_spec.targetAlpha);

        _canCommitFinalState = false;
        _rect = null;
        _group = null;
        _tween = null;
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null || _group == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _group.DOKill(false);

        ApplyFinalState(_spec.targetAlpha);

        _canCommitFinalState = false;
        _rect = null;
        _group = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (scope == null || scope.Presentation == null)
            return;

        _rect = scope.Presentation.GetRect(_spec.target);
        if (_rect == null)
            return;

        _group = GetOrAddCanvasGroup(_rect);
    }

    private void ApplyFinalState(float alpha)
    {
        if (_group == null)
            return;

        float clamped = Mathf.Clamp01(alpha);
        _group.alpha = clamped;

        if (_spec.controlInteractable)
            _group.interactable = clamped > 0.999f;

        if (_spec.controlBlocksRaycasts)
            _group.blocksRaycasts = clamped > 0.001f;
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect == null)
            return null;

        if (rect.TryGetComponent(out CanvasGroup group))
            return group;

        Debug.LogWarning($"[FadeToPresentationCommand] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}