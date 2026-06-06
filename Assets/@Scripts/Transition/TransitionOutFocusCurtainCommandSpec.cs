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

    [Header("Options")]
    public bool killTween = true;
    public bool clearOthersBeforeOut = true;
    public bool clearAllAfterOut = true;
    public bool blockRaycastWhenClosed = false;
}

public sealed class TransitionOutFocusCurtainCommand : CommandBase
{
    private readonly TransitionOutFocusCurtainCommandSpec _spec;

    private FocusBlurCurtainGraphic _graphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionOutFocusCurtainCommand(TransitionOutFocusCurtainCommandSpec spec)
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
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.FocusBlurCurtain);

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
                        _spec.blockRaycastWhenClosed && value >= 0.98f;
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

        if (provider == null || provider.FocusBlurCurtain == null)
            return;

        _graphic = provider.FocusBlurCurtain.GetComponent<FocusBlurCurtainGraphic>();
    }

    private void PrepareCoveredState()
    {
        if (_graphic == null)
            return;

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
        _graphic.RaycastBlocking = _spec.blockRaycastWhenClosed;
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
            _graphic.ClearImmediate();
        }

        if (_spec.clearAllAfterOut)
            PresentationTransitionClearUtility.ClearAll();

        _canCommitFinalState = false;
        _graphic = null;
    }
}