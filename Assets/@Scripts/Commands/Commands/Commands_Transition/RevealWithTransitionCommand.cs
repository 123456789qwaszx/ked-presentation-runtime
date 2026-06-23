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
}

public sealed class RevealWithTransitionCommand : CommandBase
{
    private readonly RevealWithTransitionCommandSpec _spec;

    private IPresentationTransitionSlotProvider _provider;
    private object _target;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => true;

    public RevealWithTransitionCommand(RevealWithTransitionCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = CreateRevealTween();
        tween.OnComplete(CommitFinalState);

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
        _provider = UIManager.Instance.GetUI<PresentationUIRoot>();
    }

    private void ClaimTarget()
    {
        _target = ResolveTarget();
        
        DOTween.Kill(_target, true);

        PrepareCoveredState();

        PresentationTransitionClearUtility.ClearAllExcept(GetLayer());

        HasClaimedTarget = true;
    }

    private object ResolveTarget()
    {
        return _spec.kind switch
        {
            PresentationRevealTransitionKind.VerticalStrip =>_provider.VerticalStripWipe.GetComponent<VerticalStripWipeGraphic>(),
            PresentationRevealTransitionKind.SlantedShutter => _provider.SlantedShutter.GetComponent<SlantedShutterGraphic>(),
            PresentationRevealTransitionKind.FocusBlurFade =>_provider.FocusBlurFade.GetComponent<FocusBlurFadeOverlay>(),
            PresentationRevealTransitionKind.FocusBlurCurtain =>_provider.FocusBlurCurtain.GetComponent<FocusBlurCurtainGraphic>(),

            _ => null
        };
    }

    private PresentationTransitionLayer GetLayer()
    {
        return _spec.kind switch
        {
            PresentationRevealTransitionKind.VerticalStrip => PresentationTransitionLayer.VerticalStripWipe,
            PresentationRevealTransitionKind.SlantedShutter => PresentationTransitionLayer.SlantedShutter,
            PresentationRevealTransitionKind.FocusBlurFade => PresentationTransitionLayer.FocusBlurFade,
            PresentationRevealTransitionKind.FocusBlurCurtain => PresentationTransitionLayer.FocusBlurCurtain,
            _ => PresentationTransitionLayer.VerticalStripWipe
        };
    }

    private void PrepareCoveredState()
    {
        switch (_target)
        {
            case VerticalStripWipeGraphic graphic:
                graphic.gameObject.SetActive(true);
                graphic.color = _spec.color;
                graphic.Configure(
                    stripCount: 20,
                    stripDelay: 0.02f,
                    stripFillDuration: 0.08f,
                    order: VerticalStripWipeOrder.RightToLeft);
                graphic.CoverImmediate();
                break;

            case SlantedShutterGraphic graphic:
                graphic.gameObject.SetActive(true);
                graphic.color = _spec.color;
                graphic.Configure(
                    slantPixels: 140f,
                    openGapHeight: 460f,
                    finalGapHeight: 0f,
                    centerBandHeight: 280f,
                    centerStartAlpha: 0.25f,
                    centerEndAlpha: 1f);
                graphic.CoverImmediate();
                graphic.RaycastBlocking = false;
                break;

            case FocusBlurFadeOverlay overlay:
                overlay.gameObject.SetActive(true);
                overlay.SetColor(_spec.color);
                overlay.CoverImmediate(1f, 0.035f);
                break;

            case FocusBlurCurtainGraphic graphic:
                graphic.gameObject.SetActive(true);
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
                graphic.RaycastBlocking = false;
                break;
        }
    }

    private Tween CreateRevealTween()
    {
        return _target switch
        {
            VerticalStripWipeGraphic graphic => DOTween
                .To(
                    () => 1f,
                    value => graphic.Progress01 = value,
                    0f,
                    _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(graphic),

            SlantedShutterGraphic graphic => DOTween
                .To(
                    () => 1f,
                    value =>
                    {
                        graphic.Progress01 = value;
                        graphic.RaycastBlocking = false;
                    },
                    0f,
                    _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(graphic),

            FocusBlurFadeOverlay overlay => DOTween
                .To(
                    () => 1f,
                    value =>
                    {
                        overlay.SetAlpha(value);
                        overlay.SetZoomAmount(0.035f * value);
                    },
                    0f,
                    _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(overlay),

            FocusBlurCurtainGraphic graphic => DOTween
                .To(
                    () => 1f,
                    value =>
                    {
                        graphic.Progress01 = value;
                        graphic.RaycastBlocking = false;
                    },
                    0f,
                    _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(graphic),

            _ => null
        };
    }

    private void CommitFinalState()
    {
        PresentationTransitionClearUtility.ClearAll();

        HasClaimedTarget = false;
    }
}