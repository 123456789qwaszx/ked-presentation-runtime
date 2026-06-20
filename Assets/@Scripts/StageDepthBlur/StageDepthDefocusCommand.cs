using System;
using System.Collections;
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
    [Range(0f, 1f)] public float edgeHide = 1f;

    [Header("Coverage")]
    [Tooltip("Blur overlay가 원본 depth layer bounds보다 더 넓게 덮는 화면 픽셀 여백. 경계에서 sharp source가 노출되는 것을 줄인다.")]
    [Min(0f)] public float coveragePaddingPixels = 48f;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;
}

// Ownership boundary:
//   Command : presentation state, including overlay alpha and character edge hide.
//   Runtime : baked image state, including RawImage binding, coverage, capture, blur, and rebakes.
// The command only asks IStageDepthLayerBlurRuntime to start or stop baking this layer.
public sealed class StageDepthDefocusCommand : CommandBase
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
    
    private const float StepFinishSpeedUpMultiplier = 1.5f;

    private readonly StageDepthDefocusCommandSpec _spec;
    private readonly IStageDepthLayerBlurRuntime _runtime;

    private bool _resolveAttempted;
    private bool _targetResolved;
    private PresentationDepthDefocusTarget _target;
    private CanvasGroup _canvasGroup;
    
    private readonly List<EdgeHideBinding> _edgeHideBindings = new();
    private readonly List<CharacterRigRefs> _rigScratch = new();

    private OverlayState _fromState;
    private OverlayState _destState;

    private Tween _tween;

    private bool HasClaimed { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public StageDepthDefocusCommand(
        StageDepthDefocusCommandSpec spec,
        IStageDepthLayerBlurRuntime runtime)
    {
        _spec = spec;
        _runtime = runtime;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_targetResolved)
            yield break;

        ClaimTarget(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    OverlayState state = OverlayState.Lerp(_fromState, _destState, t);
                    ApplyState(state, t);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_canvasGroup)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_targetResolved)
            return;

        if (!HasClaimed)
            ClaimTarget(scope);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _runtime.ResolveTarget(_spec.stage, _spec.layer, out _target);

        _canvasGroup = _target.OverlayCanvasGroup;
        _targetResolved = true;
    }

    // Captures edge-hide targets under the content root at claim time.
    // Later-mounted rigs are intentionally not included.
    private void CollectEdgeHideControllers(CommandRunScope scope)
    {
        _edgeHideBindings.Clear();

        RectTransform contentRoot = _target.SourceContentRoot;
        CharacterRigRegistry rigs = scope.CharacterRigs;

        if (contentRoot == null || rigs == null)
            return;

        _rigScratch.Clear();
        rigs.CollectAliveRigs(_rigScratch);

        for (int i = 0; i < _rigScratch.Count; i++)
        {
            CharacterRigRefs refs = _rigScratch[i];

            if (refs?.RigRoot == null || refs.VisualEffect == null)
                continue;

            if (!IsDescendantOf(refs.RigRoot, contentRoot))
                continue;

            CharacterRigVisualEffectController controller = refs.VisualEffect;

            _edgeHideBindings.Add(new EdgeHideBinding(
                controller,
                controller.StageBlurEdgeHide));
        }
    }

    private void ClaimTarget(CommandRunScope scope)
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

        HasClaimed = true;
    }

    private void CommitFinalState()
    {
        DOTween.Kill(_canvasGroup, false);

        ApplyState(_destState, 1f);

        if (!_spec.visible)
            _runtime.EndLayer(_spec.stage, _spec.layer);

        HasClaimed = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimed)
            return;

        _tween.Kill(false);

        OverlayState currentState = CaptureCurrentState();
        float duration = CalculateAcceleratedRemainingDuration(currentState);

        _fromState = currentState;
        RefreshEdgeHideFromValues();

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    OverlayState state = OverlayState.Lerp(_fromState, _destState, t);
                    ApplyState(state, t);
                },
                1f,
                duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_canvasGroup)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration(OverlayState currentState)
    {
        float originalDistance = OverlayState.Distance(_fromState, _destState);
        float remainingDistance = OverlayState.Distance(currentState, _destState);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion
    
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
    
    private void ApplyState(
        OverlayState state,
        float t)
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