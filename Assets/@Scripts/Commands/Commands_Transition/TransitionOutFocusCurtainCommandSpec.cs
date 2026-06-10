using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Transition Out - Focus Curtain",
    Order = -833)]
public sealed class TransitionOutFocusCurtainCommandSpec : CommandSpecBase
{
    [Header("Curtain Shape")]
    public float openGapHeight = 520f;
    public float finalGapHeight = 0f;
    public float slantPixels = 90f;

    [Header("Soft Edge")]
    public float edgeFeatherHeight = 140f;

    [Range(0f, 1f)]
    public float edgeFeatherAlpha = 0.55f;

    [Header("Center Blur Fake")]
    public float centerBlurHeight = 320f;

    [Range(0f, 1f)]
    public float centerStartAlpha = 0.12f;

    [Range(0f, 1f)]
    public float centerEndAlpha = 0.82f;

    public int centerBlurSlices = 18;

    [Header("Visual")]
    public Color color = Color.black;

    [Header("Tween")]
    public float duration = 0.42f;
    public Ease ease = Ease.InOutCubic;
}

public sealed class TransitionOutFocusCurtainCommand : CommandBase
{
    private readonly TransitionOutFocusCurtainCommandSpec _spec;

    private FocusBlurCurtainGraphic _graphic;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => true;

    public TransitionOutFocusCurtainCommand(TransitionOutFocusCurtainCommandSpec spec)
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
                    _graphic.Progress01 = value;
                    _graphic.RaycastBlocking = value >= 0.98f;
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_graphic)
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
        _graphic = provider.FocusBlurCurtain.GetComponent<FocusBlurCurtainGraphic>();
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_graphic, true);

        PrepareCoveredState();

        PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.FocusBlurCurtain);

        HasClaimedTarget = true;
    }

    private void PrepareCoveredState()
    {
        _graphic.gameObject.SetActive(true);
        _graphic.color = _spec.color;

        _graphic.Configure(
            _spec.openGapHeight,
            _spec.finalGapHeight,
            _spec.slantPixels,
            _spec.edgeFeatherHeight,
            _spec.edgeFeatherAlpha,
            _spec.centerBlurHeight,
            _spec.centerStartAlpha,
            _spec.centerEndAlpha,
            _spec.centerBlurSlices);

        _graphic.Progress01 = 1f;
        _graphic.RaycastBlocking = false;
    }

    private void CommitFinalState()
    {
        _graphic.ClearImmediate();

        PresentationTransitionClearUtility.ClearAll();

        HasClaimedTarget = false;
    }
}