using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Depth Visual",
    "Depth Defocus",
    Order = -120)]
public sealed class StageDepthDefocusCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Back;

    [Header("Mode")]
    public bool visible = true;

    [Header("Defocus")]
    [Range(0f, 1f)] public float alpha = 1f;
    [Range(0f, 8f)] public float blurRadius = 3f;
    [Range(1, 6)] public int iterations = 2;
    public UIStageBlurDownsample downsample = UIStageBlurDownsample.Quarter;

    [Header("Edge Hide")]
    [Tooltip("defocus 동안 원본 캐릭터 외곽을 셰이더(EdgeHide)로 지우는 양. blur overlay와 sharp source의 경계 노출을 줄인다.")]
    [Range(0f, 10f)] public float edgeHide = 8f;

    [Header("Coverage")]
    [Tooltip("Blur overlay가 원본 depth layer bounds보다 더 넓게 덮는 화면 픽셀 여백. 경계에서 sharp source가 노출되는 것을 줄인다.")]
    [Min(0f)] public float coveragePaddingPixels = 400f;

    [Header("Tween")]
    public float duration = 0.8f;
    public Ease ease = Ease.OutCubic;
}

// own presentation state, including overlay alpha and character edge hide.
// The command only asks IStageDepthLayerBlurRuntime to start or stop baking this layer.
public sealed class StageDepthDefocusCommand : ClaimTweenCommandBase
{
    private struct EdgeHideBinding
    {
        public CharacterRigVisualEffectController Controller;
        public float FromValue;

        public EdgeHideBinding(
            CharacterRigVisualEffectController controller,
            float fromValue)
        {
            Controller = controller;
            FromValue = fromValue;
        }
    }
    
    private readonly StageDepthDefocusCommandSpec _spec;
    private readonly IStageDepthLayerBlurRuntime _runtime;

    private PresentationDepthDefocusTarget _target;
    private CanvasGroup _canvasGroup;

    private readonly List<EdgeHideBinding> _edgeHideBindings = new();
    private readonly List<CharacterRigRefs> _rigScratch = new();

    private OverlayState _fromState;
    private OverlayState _destState;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 화면 절반이 한꺼번에 초점을 되찾으면 눈에 띄게 튄다 — 훨씬 완만하게 붙인다.
    protected override float StepFinishSpeedUpMultiplier => 1.5f;

    public StageDepthDefocusCommand(
        StageDepthDefocusCommandSpec spec,
        IStageDepthLayerBlurRuntime runtime)
    {
        _spec = spec;
        _runtime = runtime;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        _runtime.ResolveTarget(_spec.stage, _spec.layer, out _target);
        _canvasGroup = _target.OverlayCanvasGroup;
    }

    // Captures edge-hide targets under the content root at claim time.
    // Later-mounted rigs are intentionally not included.
    private void CollectEdgeHideControllers(CommandRunScope scope)
    {
        _edgeHideBindings.Clear();

        RectTransform contentRoot = _target.SourceContentRoot;
        CharacterRigRegistry rigs = scope.CharacterRigs;

        _rigScratch.Clear();
        rigs.CollectAliveRigs(_rigScratch);

        for (int i = 0; i < _rigScratch.Count; i++)
        {
            CharacterRigRefs refs = _rigScratch[i];

            if (!IsDescendantOf(refs.RigRoot, contentRoot))
                continue;

            _edgeHideBindings.Add(new EdgeHideBinding(
                refs.VisualEffect, 
                refs.VisualEffect.StageBlurEdgeHide));
        }
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _canvasGroup.DOKill(true);

        CollectEdgeHideControllers(scope);

        _fromState = CaptureCurrentState();
        _destState = BuildDestState();

        // Begin baking before fade-in so the overlay already has a texture.
        // Runtime keeps rebaking until EndLayer.
        if (_spec.visible)
        {
            _runtime.BeginLayer(
                _spec.stage,
                _spec.layer,
                _target,
                scope,
                BuildBlurParams());
        }
    }

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(
                () => 0f,
                t => ApplyState(OverlayState.Lerp(_fromState, _destState, t), t),
                1f,
                duration)
            .SetEase(_spec.ease)
            .SetTarget(_canvasGroup);

    /// <summary>
    /// 진행률(0→1) 트윈이라 그대로 재시작하면 처음부터 다시 돈다 —
    /// 현재 상태를 새 출발점으로 삼아 남은 구간만 태운다 (edge hide도 함께 재기준화).
    /// </summary>
    protected override Tween CreateAcceleratedTween(float duration)
    {
        _fromState = CaptureCurrentState();
        RefreshEdgeHideFromValues();

        return CreateTween(duration);
    }

    protected override void OnCommitFinalState()
    {
        DOTween.Kill(_canvasGroup, false);

        ApplyState(_destState, 1f);

        if (!_spec.visible)
            _runtime.EndLayer(_spec.stage, _spec.layer);
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            OverlayState.Distance(_fromState, _destState),
            OverlayState.Distance(CaptureCurrentState(), _destState));

    private void RefreshEdgeHideFromValues()
    {
        for (int i = 0; i < _edgeHideBindings.Count; i++)
        {
            EdgeHideBinding binding = _edgeHideBindings[i];

            if (binding.Controller == null)
                continue;

            binding.FromValue = binding.Controller.StageBlurEdgeHide;
            _edgeHideBindings[i] = binding;
        }
    }

    private OverlayState CaptureCurrentState()
    {
        return new OverlayState(_canvasGroup.alpha);
    }

    private OverlayState BuildDestState()
    {
        if (_spec.visible)
            return new OverlayState(Mathf.Clamp01(_spec.alpha));

        return new OverlayState(0f);
    }
    
    private void ApplyState(OverlayState state, float t)
    {
        _canvasGroup.alpha = state.Alpha;

        float targetEdgeHide = _spec.visible
            ? Mathf.Clamp01(_spec.edgeHide)
            : 0f;

        for (int i = 0; i < _edgeHideBindings.Count; i++)
        {
            EdgeHideBinding binding = _edgeHideBindings[i];

            if (binding.Controller == null)
                continue;

            float edgeHide = Mathf.Lerp(
                binding.FromValue,
                targetEdgeHide,
                Mathf.Clamp01(t));

            binding.Controller.SetStageBlurEdgeHideImmediate(edgeHide);
        }
    }

    private StageDepthBlurParams BuildBlurParams()
    {
        return new StageDepthBlurParams(
            _spec.blurRadius,
            _spec.iterations,
            _spec.downsample,
            _spec.coveragePaddingPixels);
    }

    private static bool IsDescendantOf(Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return false;

        Transform t = child;

        while (t != null)
        {
            if (t == parent)
                return true;

            t = t.parent;
        }

        return false;
    }

    private readonly struct OverlayState
    {
        public readonly float Alpha;

        public OverlayState(float alpha)
        {
            Alpha = Mathf.Clamp01(alpha);
        }

        public static OverlayState Lerp(
            OverlayState from,
            OverlayState to,
            float t)
        {
            t = Mathf.Clamp01(t);

            return new OverlayState(
                Mathf.Lerp(from.Alpha, to.Alpha, t));
        }

        public static float Distance(
            OverlayState from,
            OverlayState to)
        {
            return Mathf.Abs(to.Alpha - from.Alpha);
        }
    }
}