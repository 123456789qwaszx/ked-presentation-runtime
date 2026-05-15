using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "Slanted Mask Slide In",
    Order = -899)]
public sealed class SlantedMaskSlideInCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Shape")]
    public Vector2 fromOffset = new Vector2(-2200f, 0f);
    public Vector2 toOffset = new Vector2(-770f, 0f);

    [Header("Mask Shape Fixed Options")]
    public bool slantToRight = false;
    public bool flipVertical = true;

    [Header("Tween")]
    public float duration = 0.65f;
    public Ease ease = Ease.OutCubic;

    [Header("Rubber End")]
    [Tooltip("끝부분에서 진행 방향으로 살짝 지나쳤다가 목적지로 돌아오는 거리입니다.")]
    public float overshootPixels = 72f;

    [Tooltip("오버슛이 시작되는 진행률입니다. 0.75면 마지막 25% 구간에서 고무줄처럼 처리됩니다.")]
    [Range(0.01f, 0.99f)]
    public float overshootStart = 0.72f;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class SlantedMaskSlideInCommand : CommandBase, IStepScopedCommand
{
    private readonly SlantedMaskSlideInCommandSpec _spec;

    private SlantedMaskGraphic _maskGraphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlantedMaskSlideInCommand(SlantedMaskSlideInCommandSpec spec)
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
            : Vector2.right;

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
                    float rubber = RubberOvershootEnd(e, _spec.overshootStart);

                    _maskGraphic.ShapeOffsetPixels =
                        baseOffset + moveDir * (_spec.overshootPixels * rubber);
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

        RectTransform rect = PresentationTargetResolver.ResolveRect(
            scope,
            _spec.target,
            _spec.strict,
            nameof(SlantedMaskSlideInCommand));

        if (rect == null)
            return;

        _maskGraphic = rect.GetComponent<SlantedMaskGraphic>();

        if (_maskGraphic == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[SlantedMaskSlideInCommand] Target '{_spec.target}' does not have SlantedMaskGraphic.");
        }
    }

    private void ApplyFixedMaskOptions()
    {
        if (_maskGraphic == null)
            return;

        _maskGraphic.SlantToRight = _spec.slantToRight;
        _maskGraphic.FlipVertical = _spec.flipVertical;
    }

    private static float RubberOvershootEnd(float e, float overshootStart)
    {
        e = Mathf.Clamp01(e);
        overshootStart = Mathf.Clamp(overshootStart, 0.01f, 0.99f);

        if (e < overshootStart)
            return 0f;

        float t = Mathf.InverseLerp(overshootStart, 1f, e);

        // 0 -> 1 -> 0
        // 마지막 구간에서만 목적지를 살짝 지나갔다가 다시 돌아온다.
        return Mathf.Sin(t * Mathf.PI);
    }
}