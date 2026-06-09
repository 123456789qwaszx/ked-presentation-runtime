using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum ScreenVignetteMode
{
    Custom = 0,
    Preset = 1,
    Clear = 2,
    LetterBox = 3
}

public enum ScreenVignettePreset
{
    DefaultFocus = 0,
    Tension = 1,
    Horror = 2,
    Danger = 3,
    Memory = 4,
    Dream = 5
}

[Serializable]
[CommandMenuHint(
    "Screen Effect",
    "Screen Vignette",
    Order = -690,
    Sets = new[]
    {
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -690)]
public sealed class ScreenVignetteCommandSpec : CommandSpecBase
{
    [Header("Mode")]
    public ScreenVignetteMode mode = ScreenVignetteMode.Preset;
    public ScreenVignettePreset preset = ScreenVignettePreset.DefaultFocus;

    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("Custom")]
    public Color color = Color.black;

    [Range(0f, 1f)]
    public float amount = 0.4f;

    [Range(0f, 1f)]
    public float radius = 0.45f;

    [Range(0.001f, 1f)]
    public float softness = 0.35f;

    [Min(0f)]
    public float aspect = 1.777f;

    [Header("LetterBox")]
    [Tooltip("0이면 암막 없음, 1이면 상하 암막이 많이 내려옵니다.")]
    [Range(0f, 1f)]
    public float letterBoxAmount = 0.5f;

    [Range(0.001f, 0.2f)]
    public float letterBoxSoftness = 0.025f;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;
}

public sealed class ScreenVignetteCommand : CommandBase
{
    private readonly ScreenVignetteCommandSpec _spec;
    private readonly ScreenVignettePresetDBSO _presetDb;

    private ScreenVignetteEffectController _controller;

    private VignetteState _fromState;
    private VignetteState _destState;

    private bool _resolveAttempted;

    private bool HasClaimedController { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ScreenVignetteCommand(ScreenVignetteCommandSpec spec, ScreenVignettePresetDBSO presetDb)
    {
        _spec = spec;
        _presetDb = presetDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        ClaimController();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    VignetteState state = VignetteState.Lerp(_fromState, _destState, t);
                    ApplyState(state);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_controller.transform)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasClaimedController)
            ClaimController();

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        PresentationUIRoot root = UIManager.Instance.GetUI<PresentationUIRoot>();
        _controller = root.GetScreenVignetteEffect();
    }

    private void ClaimController()
    {
        DOTween.Kill(_controller.transform, true);
        _controller.KillTween(true);

        _fromState = CaptureCurrentState();
        _destState = BuildDestState();

        HasClaimedController = true;
    }
    
    private void CommitFinalState()
    {
        DOTween.Kill(_controller.transform, false);
        _controller.KillTween(false);
        
        ApplyState(_destState);

        HasClaimedController = false;
    }

    private VignetteState CaptureCurrentState()
    {
        return new VignetteState(
            _controller.Amount,
            _controller.Color,
            _controller.Radius,
            _controller.Softness,
            _controller.Aspect);
    }

    private VignetteState BuildDestState()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);

        switch (_spec.mode)
        {
            case ScreenVignetteMode.Custom:
                return new VignetteState(
                    _spec.amount * intensity,
                    _spec.color,
                    _spec.radius,
                    _spec.softness,
                    _spec.aspect);

            case ScreenVignetteMode.Preset:
                return BuildPresetState(_spec.preset, intensity);

            case ScreenVignetteMode.Clear:
                return new VignetteState(
                    0f,
                    _controller != null ? _controller.Color : Color.black,
                    _controller != null ? _controller.Radius : 0.45f,
                    _controller != null ? _controller.Softness : 0.35f,
                    _controller != null ? _controller.Aspect : 1.777f);

            case ScreenVignetteMode.LetterBox:
                return BuildLetterBoxState(_spec.letterBoxAmount, intensity);

            default:
                return new VignetteState(0f, Color.black, 0.45f, 0.35f, 1.777f);
        }
    }

    private VignetteState BuildPresetState(ScreenVignettePreset preset, float intensity)
    {
        if (_presetDb != null && _presetDb.TryGet(preset, out ScreenVignettePresetDBSO.Entry e))
        {
            return new VignetteState(
                e.amount * intensity,
                e.color,
                e.radius,
                e.softness,
                e.aspect);
        }

        return new VignetteState(0.35f * intensity, Color.black, 0.25f, 0.10f, 1.2f);
    }

    private VignetteState BuildLetterBoxState(float amount, float intensity)
    {
        float t = Mathf.Clamp01(amount) * intensity;

        ScreenVignettePresetDBSO.LetterBoxConfig lb = _presetDb != null
            ? _presetDb.LetterBox
            : ScreenVignettePresetDBSO.DefaultLetterBox();

        float radius = Mathf.Lerp(lb.radiusOpen, lb.radiusClosed, t);

        return new VignetteState(
            1f,
            lb.color,
            radius,
            Mathf.Max(0.001f, _spec.letterBoxSoftness),
            0f);
    }

    private void ApplyState(VignetteState state)
    {
        _controller.ApplyImmediate(
            state.Amount,
            state.Color,
            state.Radius,
            state.Softness,
            state.Aspect);
    }

    private readonly struct VignetteState
    {
        public readonly float Amount;
        public readonly Color Color;
        public readonly float Radius;
        public readonly float Softness;
        public readonly float Aspect;

        public VignetteState(
            float amount,
            Color color,
            float radius,
            float softness,
            float aspect)
        {
            Amount = Mathf.Clamp01(amount);
            Color = color;
            Radius = Mathf.Clamp01(radius);
            Softness = Mathf.Max(0.001f, softness);
            Aspect = Mathf.Max(0f, aspect);
        }

        public static VignetteState Lerp(VignetteState from, VignetteState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new VignetteState(
                Mathf.Lerp(from.Amount, to.Amount, t),
                Color.Lerp(from.Color, to.Color, t),
                Mathf.Lerp(from.Radius, to.Radius, t),
                Mathf.Lerp(from.Softness, to.Softness, t),
                Mathf.Lerp(from.Aspect, to.Aspect, t));
        }
    }
}