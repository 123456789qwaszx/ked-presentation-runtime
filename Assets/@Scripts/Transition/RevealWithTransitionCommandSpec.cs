using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum PresentationRevealTransitionKind
{
    VerticalStrip = 0,
    SlantedShutter = 10,
    FocusBlurFade = 20,
    FocusBlurCurtain = 30
}

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Reveal With Transition",
    Order = -839)]
public sealed class RevealWithTransitionCommandSpec : CommandSpecBase
{
    public PresentationRevealTransitionKind kind = PresentationRevealTransitionKind.VerticalStrip;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.InOutCubic;

    [Header("Visual")]
    public Color color = Color.black;

    [Header("Options")]
    public bool clearOthersBeforeReveal = true;
    public bool clearAllAfterReveal = true;
    public bool killTween = true;
}

public sealed class RevealWithTransitionCommand : CommandBase
{
    private readonly RevealWithTransitionCommandSpec _spec;

    private Tween _tween;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public RevealWithTransitionCommand(RevealWithTransitionCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        IPresentationTransitionSlotProvider provider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (provider == null)
            yield break;

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        switch (_spec.kind)
        {
            case PresentationRevealTransitionKind.VerticalStrip:
                yield return RevealVerticalStrip(provider);
                break;

            case PresentationRevealTransitionKind.SlantedShutter:
                yield return RevealSlantedShutter(provider);
                break;

            case PresentationRevealTransitionKind.FocusBlurFade:
                yield return RevealFocusBlurFade(provider);
                break;

            case PresentationRevealTransitionKind.FocusBlurCurtain:
                yield return RevealFocusBlurCurtain(provider);
                break;
        }
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        CommitFinalState();
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        CommitFinalState();
    }

    private IEnumerator RevealVerticalStrip(IPresentationTransitionSlotProvider provider)
    {
        RectTransform rect = provider.VerticalStripWipe;
        if (rect == null)
            yield break;

        VerticalStripWipeGraphic graphic = rect.GetComponent<VerticalStripWipeGraphic>();
        if (graphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(graphic, false);

        rect.gameObject.SetActive(true);

        graphic.color = _spec.color;
        graphic.Configure(
            stripCount: 20,
            stripDelay: 0.02f,
            stripFillDuration: 0.08f,
            order: VerticalStripWipeOrder.RightToLeft);

        graphic.CoverImmediate();

        if (_spec.clearOthersBeforeReveal)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.VerticalStripWipe);

        _tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    if (!_canCommitFinalState || graphic == null)
                        return;

                    graphic.Progress01 = value;
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(graphic)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    private IEnumerator RevealSlantedShutter(IPresentationTransitionSlotProvider provider)
    {
        RectTransform rect = provider.SlantedShutter;
        if (rect == null)
            yield break;

        SlantedShutterGraphic graphic = rect.GetComponent<SlantedShutterGraphic>();
        if (graphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(graphic, false);

        rect.gameObject.SetActive(true);

        graphic.color = _spec.color;
        graphic.Configure(
            slantPixels: 140f,
            openGapHeight: 460f,
            finalGapHeight: 0f,
            centerBandHeight: 280f,
            centerStartAlpha: 0.25f,
            centerEndAlpha: 1f);

        graphic.CoverImmediate();

        if (_spec.clearOthersBeforeReveal)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.SlantedShutter);

        _tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    if (!_canCommitFinalState || graphic == null)
                        return;

                    graphic.Progress01 = value;
                    graphic.RaycastBlocking = false;
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(graphic)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    private IEnumerator RevealFocusBlurFade(IPresentationTransitionSlotProvider provider)
    {
        RectTransform rect = provider.FocusBlurFade;
        if (rect == null)
            yield break;

        FocusBlurFadeOverlay overlay = rect.GetComponent<FocusBlurFadeOverlay>();
        if (overlay == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(overlay, false);

        rect.gameObject.SetActive(true);

        overlay.SetColor(_spec.color);
        overlay.CoverImmediate(1f, 0.035f);

        if (_spec.clearOthersBeforeReveal)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.FocusBlurFade);

        _tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    if (!_canCommitFinalState || overlay == null)
                        return;

                    overlay.SetAlpha(value);
                    overlay.SetZoomAmount(0.035f * value);
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(overlay)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    private IEnumerator RevealFocusBlurCurtain(IPresentationTransitionSlotProvider provider)
    {
        RectTransform rect = provider.FocusBlurCurtain;
        if (rect == null)
            yield break;

        FocusBlurCurtainGraphic graphic = rect.GetComponent<FocusBlurCurtainGraphic>();
        if (graphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(graphic, false);

        rect.gameObject.SetActive(true);

        graphic.color = _spec.color;
        graphic.Configure(
            openGapHeight: 520f,
            finalGapHeight: 0f,
            slantPixels: 90f,
            edgeFeatherHeight: 140f,
            edgeFeatherAlpha: 0.55f,
            centerBlurHeight: 320f,
            centerStartAlpha: 0.12f,
            centerEndAlpha: 0.82f,
            centerBlurSlices: 18);

        graphic.CoverImmediate();

        if (_spec.clearOthersBeforeReveal)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.FocusBlurCurtain);

        _tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    if (!_canCommitFinalState || graphic == null)
                        return;

                    graphic.Progress01 = value;
                    graphic.RaycastBlocking = false;
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(graphic)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    private void CommitFinalState()
    {
        _tween?.Kill(false);
        _tween = null;

        if (_spec.clearAllAfterReveal)
            PresentationTransitionClearUtility.ClearAll();

        _canCommitFinalState = false;
    }
}