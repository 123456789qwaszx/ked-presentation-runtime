using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum CharacterVisualFocusMode
{
    Focus = 0,
    Defocus = 1,
    Clear = 2,
    Custom = 3
}

[Serializable]
[CommandMenuHint(
    "Char Rig Visual",
    "Visual Focus",
    Order = -850,
    Sets = new[]
    {
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -850)]
public sealed class CharVisualFocusCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Mode")]
    public CharacterVisualFocusMode mode = CharacterVisualFocusMode.Focus;

    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("Focus / Defocus Preset Amounts")]
    [Tooltip("프로젝트 고정값. 한번 정하면 완료까지 바뀌지 않으므로 spec 기본값으로 둔다.")]
    [Range(0f, 1f)] public float focusOuterRimAmount = 0f;
    [Range(0f, 1f)] public float focusInnerRimAmount = 0.25f;
    [Range(0f, 1f)] public float defocusDimAmount = 0.45f;
    [Range(0f, 1f)] public float defocusBlurAmount = 0.25f;

    [Header("Custom Values")]
    [Range(0f, 3f)] public float dim = 0f;
    
    public Color dimTintColor = new Color(0.45f, 0.48f, 0.55f, 1f);

    [Tooltip("Legacy/custom rim amount. In the new UI shader this maps to Outer Rim.")]
    [Range(0f, 1f)] public float rim = 0f;

    [Range(0f, 1f)] public float innerRim = 0f;
    [Range(0f, 1f)] public float blur = 0f;

    public Color rimColor = Color.white;
    public Color innerRimColor = new Color(1f, 0.96f, 0.86f, 1f);

    [Header("Tween")]
    public float duration = 0.25f;
    public Ease ease = Ease.OutCubic;
}

public sealed class CharVisualFocusCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly CharVisualFocusCommandSpecCharR _spec;

    private CharacterRigVisualEffectController _controller;

    private VisualState _fromState;
    private VisualState _destState;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedController { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public CharVisualFocusCommandCharR(CharVisualFocusCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_controller == null)
            yield break;

        ClaimController();

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
                    VisualState state = VisualState.Lerp(_fromState, _destState, t);
                    ApplyState(state);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_controller)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_controller == null)
            return;

        if (!HasClaimedController)
            ClaimController();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        _controller = rigRefs?.VisualEffect;

        if (_controller != null)
            return;

        Debug.LogWarning(
            $"[CharVisualFocusCommandCharR] VisualEffect controller is missing. " +
            $"SetupCharRig가 컨트롤러를 생성했는지, source material 경로가 맞는지 확인하세요. " +
            $"slotKey='{_spec.slotKey}'.");
    }

    private void ClaimController()
    {
        DOTween.Kill(_controller, false);

        _fromState = CaptureCurrentState();
        _destState = BuildDestState();

        HasClaimedController = true;
    }

    private void CommitFinalState()
    {
        DOTween.Kill(_controller, false);

        ApplyState(_destState);

        HasClaimedController = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        _tween.Kill(false);

        VisualState currentState = CaptureCurrentState();
        float duration = CalculateAcceleratedRemainingDuration(currentState);

        _fromState = currentState;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    VisualState state = VisualState.Lerp(_fromState, _destState, t);
                    ApplyState(state);
                },
                1f,
                duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_controller)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration(VisualState currentState)
    {
        float originalDistance = VisualState.Distance(_fromState, _destState);
        float remainingDistance = VisualState.Distance(currentState, _destState);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion

    private VisualState CaptureCurrentState()
    {
        return new VisualState(
            _controller.DimAmount,
            _controller.DimTintColor,
            _controller.OuterRimAmount,
            _controller.InnerRimAmount,
            _controller.BlurAmount,
            _controller.OuterRimColor,
            _controller.InnerRimColor);
    }

    private VisualState BuildDestState()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);

        switch (_spec.mode)
        {
            case CharacterVisualFocusMode.Focus:
                return new VisualState(
                    0f,
                    _controller.DimTintColor,
                    _spec.focusOuterRimAmount * intensity,
                    _spec.focusInnerRimAmount * intensity,
                    0f,
                    _controller.OuterRimColor,
                    _controller.InnerRimColor);

            case CharacterVisualFocusMode.Defocus:
                return new VisualState(
                    _spec.defocusDimAmount * intensity,
                    _controller.DimTintColor,
                    0f,
                    0f,
                    _spec.defocusBlurAmount * intensity,
                    _controller.OuterRimColor,
                    _controller.InnerRimColor);

            case CharacterVisualFocusMode.Clear:
                return new VisualState(
                    0f,
                    _controller.DimTintColor,
                    0f,
                    0f,
                    0f,
                    _controller.OuterRimColor,
                    _controller.InnerRimColor);

            case CharacterVisualFocusMode.Custom:
                return new VisualState(
                    _spec.dim,
                    _spec.dimTintColor,
                    _spec.rim,
                    _spec.innerRim,
                    _spec.blur,
                    _spec.rimColor,
                    _spec.innerRimColor);

            default:
                return new VisualState(
                    0f,
                    _controller.DimTintColor,
                    0f,
                    0f,
                    0f,
                    _controller.OuterRimColor,
                    _controller.InnerRimColor);
        }
    }

    private void ApplyState(VisualState state)
    {
        _controller.ApplyImmediate(
            state.Dim,
            state.DimTintColor,
            state.OuterRim,
            state.InnerRim,
            state.Blur,
            state.OuterRimColor,
            state.InnerRimColor);
    }

    private readonly struct VisualState
    {
        public readonly float Dim;
        public readonly Color DimTintColor;
        public readonly float OuterRim;
        public readonly float InnerRim;
        public readonly float Blur;
        public readonly Color OuterRimColor;
        public readonly Color InnerRimColor;

        public VisualState(
            float dim,
            Color dimTintColor,
            float outerRim,
            float innerRim,
            float blur,
            Color outerRimColor,
            Color innerRimColor)
        {
            Dim = dim;
            DimTintColor = dimTintColor;
            OuterRim = Mathf.Clamp01(outerRim);
            InnerRim = Mathf.Clamp01(innerRim);
            Blur = Mathf.Clamp01(blur);
            OuterRimColor = outerRimColor;
            InnerRimColor = innerRimColor;
        }

        public static VisualState Lerp(VisualState from, VisualState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new VisualState(
                Mathf.Lerp(from.Dim, to.Dim, t),
                Color.Lerp(from.DimTintColor, to.DimTintColor, t),
                Mathf.Lerp(from.OuterRim, to.OuterRim, t),
                Mathf.Lerp(from.InnerRim, to.InnerRim, t),
                Mathf.Lerp(from.Blur, to.Blur, t),
                Color.Lerp(from.OuterRimColor, to.OuterRimColor, t),
                Color.Lerp(from.InnerRimColor, to.InnerRimColor, t));
        }

        public static float Distance(VisualState from, VisualState to)
        {
            float dim = Mathf.Abs(to.Dim - from.Dim);
            float outerRim = Mathf.Abs(to.OuterRim - from.OuterRim);
            float innerRim = Mathf.Abs(to.InnerRim - from.InnerRim);
            float blur = Mathf.Abs(to.Blur - from.Blur);

            float dimTint = ColorDistance(from.DimTintColor, to.DimTintColor);
            float outerRimColor = ColorDistance(from.OuterRimColor, to.OuterRimColor);
            float innerRimColor = ColorDistance(from.InnerRimColor, to.InnerRimColor);

            return dim +
                   outerRim +
                   innerRim +
                   blur +
                   dimTint +
                   outerRimColor +
                   innerRimColor;
        }

        private static float ColorDistance(Color from, Color to)
        {
            float r = to.r - from.r;
            float g = to.g - from.g;
            float b = to.b - from.b;
            float a = to.a - from.a;

            return Mathf.Sqrt(r * r + g * g + b * b + a * a);
        }
    }
}