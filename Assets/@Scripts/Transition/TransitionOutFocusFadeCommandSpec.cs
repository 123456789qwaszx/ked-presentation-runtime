using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Transition Out - Focus Fade",
    Order = -834)]
public sealed class TransitionOutFocusFadeCommandSpec : CommandSpecBase
{
    [Header("Visual")]
    public Color color = Color.black;

    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Header("Fake Focus Blur")]
    public float zoomAmount = 0.035f;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.InOutCubic;

    [Header("Options")]
    public bool killTween = true;
    public bool clearOthersBeforeOut = true;
    public bool clearAllAfterOut = true;
    public bool blockRaycastWhenVisible = false;
}

public sealed class TransitionOutFocusFadeCommand : CommandBase
{
    private readonly TransitionOutFocusFadeCommandSpec _spec;

    private FocusBlurFadeOverlay _overlay;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionOutFocusFadeCommand(TransitionOutFocusFadeCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_overlay == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(_overlay, false);

        PrepareCoveredState();

        if (_spec.clearOthersBeforeOut)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.FocusBlurFade);

        _canCommitFinalState = true;

        if (scope.IsSeekPassThrough || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    if (!_canCommitFinalState || _overlay == null)
                        return;

                    _overlay.SetAlpha(_spec.maxAlpha * value);
                    _overlay.SetZoomAmount(_spec.zoomAmount * value);
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_overlay)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState)
                    return;

                CommitFinalState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        CommitFinalState();
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider provider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (provider == null || provider.FocusBlurFade == null)
            return;

        _overlay = provider.FocusBlurFade.GetComponent<FocusBlurFadeOverlay>();
    }

    private void PrepareCoveredState()
    {
        if (_overlay == null)
            return;

        _overlay.gameObject.SetActive(true);
        _overlay.SetColor(_spec.color);
        _overlay.BlockRaycastWhenVisible = _spec.blockRaycastWhenVisible;
        _overlay.CoverImmediate(_spec.maxAlpha, _spec.zoomAmount);
    }

    private void CommitFinalState()
    {
        if (_tween != null)
        {
            _tween.Kill(false);
            _tween = null;
        }

        if (_overlay != null)
        {
            DOTween.Kill(_overlay, false);
            _overlay.ClearImmediate();
        }

        if (_spec.clearAllAfterOut)
            PresentationTransitionClearUtility.ClearAll();

        _canCommitFinalState = false;
        _overlay = null;
    }
}