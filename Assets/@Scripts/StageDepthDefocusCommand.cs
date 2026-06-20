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

//   - Command가 target handle을 resolve하고, from/dest visual state를 들고, DOTween을 만든다.
//   - CommitFinalState에서 overlay alpha와 edge hide 최종값을 직접 쓴다(세 경로 수렴점).
//   - Baker(IStageDepthLayerBlurRuntime)에는 "이 layer를 굽고 있어라/멈춰라"만 위임.
//
// 소유권:
//   Command  : OverlayCanvasGroup.alpha + 캐릭터 edge hide (single-writer: canvasGroup.DOKill(true)).
//   Baker    : RawImage(texture/uvRect/enabled), coverage 기하, 캡처·블러, 매 프레임 추적 재bake.
public sealed class StageDepthDefocusCommand : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 1.5f;

    private readonly StageDepthDefocusCommandSpec _spec;
    private readonly IStageDepthLayerBlurRuntime _runtime;

    // resolve 결과(한 번만). rig는 run 사이에 파괴/재빌드되므로 run 간 캐싱하지 않는다.
    private bool _resolveAttempted;
    private bool _targetResolved;
    private PresentationDepthDefocusTarget _target;
    private CanvasGroup _canvasGroup;

    // edge hide 대상: 해당 layer content 아래 살아있는 캐릭터 rig의 visual effect 컨트롤러.
    private readonly List<CharacterRigVisualEffectController> _edgeHideControllers = new();
    private readonly List<CharacterRigRefs> _rigScratch = new();

    private DefocusState _fromState;
    private DefocusState _destState;

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
                    DefocusState state = DefocusState.Lerp(_fromState, _destState, t);
                    ApplyState(state);
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

        if (!_runtime.TryResolveTarget(_spec.stage, _spec.layer, out _target) || !_target.IsValid)
        {
            Debug.LogWarning(
                $"[StageDepthDefocusCommand] depth defocus target resolve 실패. " +
                $"PresentationUIRoot의 depth overlay refs가 바인딩됐는지 확인하세요. " +
                $"stage='{_spec.stage}' layer='{_spec.layer}'.");
            return;
        }

        _canvasGroup = _target.OverlayCanvasGroup;
        _targetResolved = true;
    }

    // content root 아래 살아있는 캐릭터 rig의 visual effect 컨트롤러만 수집(edge hide 쓰기 대상).
    // 주의: defocus가 지속되는 동안 새로 mount된 rig는 이 set에 없다(claim 시점 스냅샷).
    //       blur 자체는 Baker가 매 프레임 재수집하므로 흐려지지만, 그 rig의 외곽 edge hide는 적용되지 않는다.
    //       이는 지속 defocus 모델의 알려진 한계이며, 필요 시 별도 처리한다.
    private void CollectEdgeHideControllers(CommandRunScope scope)
    {
        _edgeHideControllers.Clear();

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

            _edgeHideControllers.Add(refs.VisualEffect);
        }
    }

    private void ClaimTarget(CommandRunScope scope)
    {
        // 단일 라이터: 같은 overlay CanvasGroup을 쓰던 이전 defocus tween을 즉시 final로 커밋.
        _canvasGroup.DOKill(true);

        CollectEdgeHideControllers(scope);
        
        _fromState = CaptureCurrentState();
        _destState = BuildDestState();

        // visible이면 Baker가 추적/굽기를 시작해 텍스처를 즉시 준비한다.
        // hidden은 여기서 끄지 않는다. fade가 끝난 뒤 CommitFinalState에서 EndLayer한다
        // (fade 동안 Baker가 계속 굽고 있어야 텍스처가 살아있다).
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

        ApplyState(_destState);

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

        DefocusState currentState = CaptureCurrentState();
        float duration = CalculateAcceleratedRemainingDuration(currentState);

        _fromState = currentState;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    DefocusState state = DefocusState.Lerp(_fromState, _destState, t);
                    ApplyState(state);
                },
                1f,
                duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_canvasGroup)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration(DefocusState currentState)
    {
        float originalDistance = DefocusState.Distance(_fromState, _destState);
        float remainingDistance = DefocusState.Distance(currentState, _destState);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion

    private DefocusState CaptureCurrentState()
    {
        float alpha = _canvasGroup != null ? _canvasGroup.alpha : 0f;

        // edge hide는 layer 내 컨트롤러들이 항상 함께 설정되므로 첫 유효 컨트롤러 값을 대표로 읽는다.
        float edgeHide = 0f;
        for (int i = 0; i < _edgeHideControllers.Count; i++)
        {
            CharacterRigVisualEffectController controller = _edgeHideControllers[i];

            if (controller != null)
            {
                edgeHide = controller.StageBlurEdgeHide;
                break;
            }
        }

        return new DefocusState(alpha, edgeHide);
    }

    private DefocusState BuildDestState()
    {
        if (_spec.visible)
            return new DefocusState(Mathf.Clamp01(_spec.alpha), Mathf.Clamp01(_spec.edgeHide));

        return new DefocusState(0f, 0f);
    }

    private void ApplyState(DefocusState state)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = state.Alpha;

        for (int i = 0; i < _edgeHideControllers.Count; i++)
            _edgeHideControllers[i]?.SetStageBlurEdgeHideImmediate(state.EdgeHide);
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

    // overlay alpha + edge hide 두 축의 visual state.
    private readonly struct DefocusState
    {
        public readonly float Alpha;
        public readonly float EdgeHide;

        public DefocusState(float alpha, float edgeHide)
        {
            Alpha = Mathf.Clamp01(alpha);
            EdgeHide = Mathf.Clamp01(edgeHide);
        }

        public static DefocusState Lerp(DefocusState from, DefocusState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new DefocusState(
                Mathf.Lerp(from.Alpha, to.Alpha, t),
                Mathf.Lerp(from.EdgeHide, to.EdgeHide, t));
        }

        public static float Distance(DefocusState from, DefocusState to)
        {
            return Mathf.Abs(to.Alpha - from.Alpha) +
                   Mathf.Abs(to.EdgeHide - from.EdgeHide);
        }
    }
}