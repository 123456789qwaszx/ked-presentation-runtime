using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "Stage Mask Motion",
    Order = -899)]
public sealed class StageMaskMotionCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage01;

    [Header("Shape")]
    public StageMaskKind kind = StageMaskKind.Slanted;

    [Header("Motion")]
    public Vector2 fromOffset = new(-2200f, 0f);
    public Vector2 toOffset = Vector2.zero;

    [Header("Slanted")]
    public float slantPixels = 220f;
    public bool slantToRight = false;
    public bool flipVertical = true;

    [Header("Horizontal Strip")]
    public float stripHeightPixels = 360f;
    public float horizontalBleedPixels = 80f;

    [Header("Vertical Strip")]
    public float verticalStripWidthPixels = 460f;
    public float verticalBleedPixels = 80f;

    [Header("Diagonal Band")]
    public float diagonalBandWidthPixels = 680f;
    public float diagonalBandSlantPixels = 420f;
    public float diagonalBandBleedPixels = 260f;
    public bool diagonalBandToRight = true;

    [Header("Circle Iris")]
    public float fromIrisRadiusPixels = 0f;
    public float toIrisRadiusPixels = 1280f;
    public float irisAspect = 1f;
    [Range(12, 128)] public int irisSegments = 64;

    [Header("Edge")]
    public bool showEdge = true;
    public StageMaskEdgeMode edgeMode = StageMaskEdgeMode.Leading;
    public Color edgeColor = Color.white;
    public float edgeThickness = 6f;
    public bool hideEdgeOnComplete;

    [Header("Tween")]
    public float duration = 0.65f;
    public Ease ease = Ease.OutCubic;

    [Header("Rubber")]
    public StageMaskRubberMode rubberMode = StageMaskRubberMode.OvershootEnd;

    [Tooltip("OvershootEnd일 때 마지막 구간에서 진행 방향으로 더 밀리는 거리입니다.")]
    public float overshootPixels = 72f;

    [Range(0.01f, 0.99f)]
    public float overshootStart = 0.72f;

    [Tooltip("PullStart일 때 시작 구간에서 반대 방향으로 당기는 거리입니다.")]
    public float pullPixels = 24f;

    [Range(0.01f, 0.99f)]
    public float pullEnd = 0.28f;

    [Header("Completion")]
    public bool fullVisibleOnComplete;
}

public sealed class StageMaskMotionCommand : CommandBase
{
    private readonly StageMaskMotionCommandSpec _spec;

    private StageMaskSlot _slot;
    private StageMaskGraphic _graphic;
    private StageMaskEdgeGraphic _edgeGraphic;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public StageMaskMotionCommand(StageMaskMotionCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_graphic == null || _slot == null)
            yield break;

        ClaimTarget();

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
            : Vector2.right;

        _graphic.ShapeOffsetPixels = start;
        ApplyShapeAt(0f);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    Vector2 baseOffset = Vector2.LerpUnclamped(start, dest, e);
                    Vector2 rubberOffset = CalculateRubberOffset(e, moveDir);

                    _graphic.ShapeOffsetPixels = baseOffset + rubberOffset;
                    ApplyShapeAt(e);
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_graphic)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_graphic == null || _slot == null)
            return;

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        IStageMaskProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();

        if (!provider.TryGetStageMaskSlot(_spec.stage, out _slot) || _slot == null)
        {
            Debug.LogWarning(
                $"[StageMaskMotionCommand] StageMaskSlot is missing. " +
                $"stage='{_spec.stage}'.");
            return;
        }

        _graphic = _slot.Graphic;
        _edgeGraphic = _slot.EdgeGraphic;

        if (_graphic == null)
        {
            Debug.LogWarning(
                $"[StageMaskMotionCommand] StageMaskGraphic is missing. " +
                $"stage='{_spec.stage}'.");
        }
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_graphic, true);

        if (_edgeGraphic != null)
            DOTween.Kill(_edgeGraphic, true);

        _slot.ActivateMasked();

        ApplyFixedShapeOptions();
        ConfigureEdge();

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        ApplyFixedShapeOptions();
        ApplyShapeAt(1f);

        _graphic.ShapeOffsetPixels = _spec.toOffset;

        if (_spec.fullVisibleOnComplete)
            _slot.SetFullVisible();

        if (_spec.hideEdgeOnComplete)
            _slot.SetEdgeVisible(false);

        HasClaimedTarget = false;
        _tween = null;
    }

    private void ApplyFixedShapeOptions()
    {
        switch (_spec.kind)
        {
            case StageMaskKind.FullRect:
                _graphic.SetFullRect();
                break;

            case StageMaskKind.Slanted:
                _graphic.SetSlanted(
                    _spec.slantPixels,
                    _spec.slantToRight,
                    _spec.flipVertical);
                break;

            case StageMaskKind.HorizontalStrip:
                _graphic.SetHorizontalStrip(
                    _spec.stripHeightPixels,
                    _spec.horizontalBleedPixels);
                break;

            case StageMaskKind.VerticalStrip:
                _graphic.SetVerticalStrip(
                    _spec.verticalStripWidthPixels,
                    _spec.verticalBleedPixels);
                break;

            case StageMaskKind.DiagonalBand:
                _graphic.SetDiagonalBand(
                    _spec.diagonalBandWidthPixels,
                    _spec.diagonalBandSlantPixels,
                    _spec.diagonalBandBleedPixels,
                    _spec.diagonalBandToRight);
                break;

            case StageMaskKind.CircleIris:
                _graphic.SetCircleIris(
                    _spec.fromIrisRadiusPixels,
                    _spec.irisAspect,
                    _spec.irisSegments);
                break;
        }
    }

    private void ApplyShapeAt(float t)
    {
        t = Mathf.Clamp01(t);

        if (_spec.kind != StageMaskKind.CircleIris)
            return;

        float radius = Mathf.LerpUnclamped(
            _spec.fromIrisRadiusPixels,
            _spec.toIrisRadiusPixels,
            t);

        _graphic.SetCircleIris(
            radius,
            _spec.irisAspect,
            _spec.irisSegments);
    }

    private void ConfigureEdge()
    {
        if (_edgeGraphic == null)
            return;

        _slot.ConfigureEdge(
            _spec.edgeMode,
            _spec.edgeColor,
            _spec.edgeThickness);

        _slot.SetEdgeVisible(_spec.showEdge);
    }

    private Vector2 CalculateRubberOffset(
        float easedProgress,
        Vector2 moveDir)
    {
        switch (_spec.rubberMode)
        {
            case StageMaskRubberMode.OvershootEnd:
            {
                float rubber = RubberOvershootEnd(
                    easedProgress,
                    _spec.overshootStart);

                return moveDir * (_spec.overshootPixels * rubber);
            }

            case StageMaskRubberMode.PullStart:
            {
                float pull = RubberPullStart(
                    easedProgress,
                    _spec.pullEnd);

                return -moveDir * (_spec.pullPixels * pull);
            }

            case StageMaskRubberMode.None:
            default:
                return Vector2.zero;
        }
    }

    private static float RubberOvershootEnd(
        float e,
        float overshootStart)
    {
        e = Mathf.Clamp01(e);
        overshootStart = Mathf.Clamp(overshootStart, 0.01f, 0.99f);

        if (e < overshootStart)
            return 0f;

        float t = Mathf.InverseLerp(overshootStart, 1f, e);
        return Mathf.Sin(t * Mathf.PI);
    }

    private static float RubberPullStart(
        float e,
        float pullEnd)
    {
        e = Mathf.Clamp01(e);
        pullEnd = Mathf.Clamp(pullEnd, 0.01f, 0.99f);

        if (e >= pullEnd)
            return 0f;

        float t = Mathf.InverseLerp(0f, pullEnd, e);
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }
}