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

    [Header("Options")]
    public bool killTween = true;
    public bool clearOthersBeforeOut = true;
    public bool clearAllAfterOut = true;
    public bool blockRaycastWhileClosed = false;
}

public sealed class TransitionOutShutterCommand : CommandBase
{
    private readonly TransitionOutShutterCommandSpec _spec;

    private SlantedShutterGraphic _graphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionOutShutterCommand(TransitionOutShutterCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_graphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(_graphic, false);

        PrepareCoveredState();

        if (_spec.clearOthersBeforeOut)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.SlantedShutter);

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    if (!_canCommitFinalState || _graphic == null)
                        return;

                    _graphic.Progress01 = value;
                    _graphic.RaycastBlocking =
                        _spec.blockRaycastWhileClosed && value >= 0.98f;
                },
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_graphic)
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

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
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

        if (provider == null || provider.SlantedShutter == null)
            return;

        _graphic = provider.SlantedShutter.GetComponent<SlantedShutterGraphic>();
    }

    private void PrepareCoveredState()
    {
        if (_graphic == null)
            return;

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
        _graphic.RaycastBlocking = _spec.blockRaycastWhileClosed;
    }

    private void CommitFinalState()
    {
        if (_tween != null)
        {
            _tween.Kill(false);
            _tween = null;
        }

        if (_graphic != null)
        {
            DOTween.Kill(_graphic, false);
            _graphic.Progress01 = 0f;
            _graphic.RaycastBlocking = false;
            _graphic.gameObject.SetActive(false);
        }

        if (_spec.clearAllAfterOut)
            PresentationTransitionClearUtility.ClearAll();

        _canCommitFinalState = false;
        _graphic = null;
    }
}