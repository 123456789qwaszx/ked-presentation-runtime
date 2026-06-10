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
}

public sealed class TransitionOutFocusFadeCommand : CommandBase
{
    private readonly TransitionOutFocusFadeCommandSpec _spec;

    private FocusBlurFadeOverlay _overlay;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => true;

    public TransitionOutFocusFadeCommand(TransitionOutFocusFadeCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        ClaimTarget();

        if (scope.IsSeekPassThrough || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    _overlay.SetAlpha(_spec.maxAlpha * value);
                    _overlay.SetZoomAmount(_spec.zoomAmount * value);
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_overlay)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();
        _overlay = provider.FocusBlurFade.GetComponent<FocusBlurFadeOverlay>();
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_overlay, true);

        PrepareCoveredState();

        PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.FocusBlurFade);
        HasClaimedTarget = true;
    }

    private void PrepareCoveredState()
    {
        _overlay.gameObject.SetActive(true);
        _overlay.SetColor(_spec.color);
        _overlay.BlockRaycastWhenVisible = false;
        _overlay.CoverImmediate(_spec.maxAlpha, _spec.zoomAmount);
    }

    private void CommitFinalState()
    {
        _overlay.ClearImmediate();

        PresentationTransitionClearUtility.ClearAll();

        HasClaimedTarget = false;
    }
}