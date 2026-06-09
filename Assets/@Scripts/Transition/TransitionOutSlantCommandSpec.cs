using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Transition Out - Slant",
    Order = -835)]
public sealed class TransitionOutSlantCommandSpec : CommandSpecBase
{
    [Header("Shape")]
    public Vector2 fromOffset = new Vector2(-770f, 0f);
    public Vector2 toOffset = new Vector2(-2200f, 0f);

    [Header("Mask Shape Fixed Options")]
    public bool slantToRight = false;
    public bool flipVertical = true;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.InCubic;

    [Header("Rubber Start")]
    public float pullPixels = 0f;

    [Range(0.01f, 0.99f)]
    public float pullEnd = 0.28f;

    [Header("Options")]
    public bool killTween = true;
    public bool clearOthersBeforeOut = true;
    public bool clearAllAfterOut = true;
}

public sealed class TransitionOutSlantCommand : CommandBase
{
    private readonly TransitionOutSlantCommandSpec _spec;

    private SlantedMaskGraphic _maskGraphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionOutSlantCommand(TransitionOutSlantCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_maskGraphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(_maskGraphic, false);

        PrepareCoveredState();

        if (_spec.clearOthersBeforeOut)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.SlantedMask);

        _canCommitFinalState = true;

        if (scope.IsSeekPassThrough || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 start = _spec.fromOffset;
        Vector2 dest = _spec.toOffset;

        Vector2 moveDir = dest - start;
        moveDir = moveDir.sqrMagnitude > 0f
            ? moveDir.normalized
            : Vector2.left;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _maskGraphic == null)
                        return;

                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
                    Vector2 baseOffset = Vector2.LerpUnclamped(start, dest, e);
                    float pull = RubberPullStart(e, _spec.pullEnd);

                    _maskGraphic.ShapeOffsetPixels =
                        baseOffset - moveDir * (_spec.pullPixels * pull);
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_maskGraphic)
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

        if (provider == null || provider.SlantedMaskEdgeGraphic == null)
            return;

        _maskGraphic = provider.SlantedMaskEdgeGraphic.GetComponent<SlantedMaskGraphic>();
    }

    private void PrepareCoveredState()
    {
        if (_maskGraphic == null)
            return;

        _maskGraphic.SlantToRight = _spec.slantToRight;
        _maskGraphic.FlipVertical = _spec.flipVertical;
        _maskGraphic.ShapeOffsetPixels = _spec.fromOffset;
    }

    private void CommitFinalState()
    {
        if (_tween != null)
        {
            _tween.Kill(false);
            _tween = null;
        }

        if (_maskGraphic != null)
        {
            DOTween.Kill(_maskGraphic, false);
            _maskGraphic.SlantToRight = _spec.slantToRight;
            _maskGraphic.FlipVertical = _spec.flipVertical;
            _maskGraphic.ShapeOffsetPixels = _spec.toOffset;
        }

        if (_spec.clearAllAfterOut)
            PresentationTransitionClearUtility.ClearAll();

        _canCommitFinalState = false;
        _maskGraphic = null;
    }

    private static float RubberPullStart(float e, float pullEnd)
    {
        e = Mathf.Clamp01(e);
        pullEnd = Mathf.Clamp(pullEnd, 0.01f, 0.99f);

        if (e >= pullEnd)
            return 0f;

        float t = Mathf.InverseLerp(0f, pullEnd, e);
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }
}