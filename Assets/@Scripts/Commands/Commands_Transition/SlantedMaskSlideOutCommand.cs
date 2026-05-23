using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "Slanted Mask Slide Out",
    Order = -898)]
public sealed class SlantedMaskSlideOutCommandSpec : CommandSpecBase
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
    [Tooltip("시작할 때 반대 방향으로 살짝 당겼다가 빠져나가는 거리입니다.")]
    public float pullPixels = 24f;

    [Tooltip("당김이 사라지는 진행률입니다. 0.25면 초반 25% 구간에서만 당김이 적용됩니다.")]
    [Range(0.01f, 0.99f)]
    public float pullEnd = 0.28f;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class SlantedMaskSlideOutCommand : CommandBase, IStepScopedCommand
{
    private readonly SlantedMaskSlideOutCommandSpec _spec;

    private SlantedMaskGraphic _maskGraphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlantedMaskSlideOutCommand(SlantedMaskSlideOutCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_maskGraphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(_maskGraphic, true);

        ApplyFixedMaskOptions();

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 start = _spec.fromOffset;
        Vector2 dest = _spec.toOffset;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 moveDir = dest - start;
        moveDir = moveDir.sqrMagnitude > 0f
            ? moveDir.normalized
            : Vector2.left;

        _maskGraphic.ShapeOffsetPixels = start;

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
                if (!_canCommitFinalState || _maskGraphic == null)
                    return;

                CommitFinalState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_maskGraphic == null)
            return;

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _maskGraphic == null)
            return;

        _tween?.Kill(false);
        DOTween.Kill(_maskGraphic, false);

        CommitFinalState();
    }

    private void CommitFinalState()
    {
        if (_maskGraphic != null)
        {
            ApplyFixedMaskOptions();
            _maskGraphic.ShapeOffsetPixels = _spec.toOffset;
        }

        _canCommitFinalState = false;
        _maskGraphic = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider transitionSlotProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
        RectTransform rect = transitionSlotProvider.SlantedMaskEdgeGraphic;
        if (rect == null)
            return;

        _maskGraphic = rect.GetComponent<SlantedMaskGraphic>();
    }

    private void ApplyFixedMaskOptions()
    {
        if (_maskGraphic == null)
            return;

        _maskGraphic.SlantToRight = _spec.slantToRight;
        _maskGraphic.FlipVertical = _spec.flipVertical;
    }

    private static float RubberPullStart(float e, float pullEnd)
    {
        e = Mathf.Clamp01(e);
        pullEnd = Mathf.Clamp(pullEnd, 0.01f, 0.99f);

        if (e >= pullEnd)
            return 0f;

        float t = Mathf.InverseLerp(0f, pullEnd, e);

        // 1 -> 0
        // 시작 지점에서 살짝 반대로 당겨져 있다가 빠르게 원래 진행으로 합류한다.
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }
}