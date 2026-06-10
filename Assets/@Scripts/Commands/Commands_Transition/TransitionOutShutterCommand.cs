using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Transition Out - Shutter",
    Order = -837)]
public sealed class TransitionOutShutterCommandSpec : CommandSpecBase
{
    [Header("Shape")]
    public float slantPixels = 140f;
    public float openGapHeight = 460f;
    public float finalGapHeight = 0f;

    [Header("Center Exposure")]
    public float centerBandHeight = 280f;

    [Range(0f, 1f)]
    public float centerStartAlpha = 0.25f;

    [Range(0f, 1f)]
    public float centerEndAlpha = 1f;

    [Header("Visual")]
    public Color color = Color.black;

    [Header("Tween")]
    public float duration = 0.32f;
    public Ease ease = Ease.InCubic;
}

public sealed class TransitionOutShutterCommand : CommandBase
{
    private readonly TransitionOutShutterCommandSpec _spec;

    private SlantedShutterGraphic _graphic;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => true;

    public TransitionOutShutterCommand(TransitionOutShutterCommandSpec spec)
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

        if (_graphic == null)
            return;

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider provider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (provider == null || provider.SlantedShutter == null)
            return;

        _graphic = provider.SlantedShutter.GetComponent<SlantedShutterGraphic>();
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_graphic, true);

        PrepareCoveredState();

        PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.SlantedShutter);

        HasClaimedTarget = true;
    }

    private void PrepareCoveredState()
    {
        _graphic.gameObject.SetActive(true);
        _graphic.color = _spec.color;

        _graphic.Configure(
            _spec.slantPixels,
            _spec.openGapHeight,
            _spec.finalGapHeight,
            _spec.centerBandHeight,
            _spec.centerStartAlpha,
            _spec.centerEndAlpha);

        _graphic.Progress01 = 1f;
        _graphic.RaycastBlocking = false;
    }

    private void CommitFinalState()
    {
        _graphic.Progress01 = 0f;
        _graphic.RaycastBlocking = false;
        _graphic.gameObject.SetActive(false);

        PresentationTransitionClearUtility.ClearAll();

        HasClaimedTarget = false;
    }
}