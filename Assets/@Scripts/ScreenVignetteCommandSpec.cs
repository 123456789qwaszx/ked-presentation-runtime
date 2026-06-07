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

    [Header("Options")]
    public bool killTween = true;
    public bool clearOnSkipOrRollback = false;
}

public sealed class ScreenVignetteCommand : CommandBase
{
    private readonly ScreenVignetteCommandSpec _spec;

    private ScreenVignetteEffectController _controller;
    private Tween _tween;

    private bool _resolveAttempted;
    private bool _canApply;

    private VignetteState _fromState;
    private VignetteState _destState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ScreenVignetteCommand(ScreenVignetteCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_controller == null)
            yield break;

        if (_spec.killTween)
            _controller.KillTween(true);

        _fromState = CaptureCurrentState();
        _destState = BuildDestState();

        _canApply = true;

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
                    if (!_canApply || _controller == null)
                        return;

                    VignetteState state = VignetteState.Lerp(_fromState, _destState, t);
                    ApplyState(state);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_controller.transform)
            .OnComplete(() =>
            {
                if (!_canApply || _controller == null)
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

        if (_controller == null)
            return;

        _tween?.Kill(false);
        _controller.KillTween(false);

        if (_spec.clearOnSkipOrRollback)
        {
            _controller.ClearImmediate();
            ClearRuntimeRefs();
            return;
        }

        _destState = BuildDestState();
        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!_canApply || _controller == null)
            return;

        _tween?.Kill(false);
        _controller.KillTween(false);

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        PresentationUIRoot root = UIManager.Instance.GetUI<PresentationUIRoot>();

        if (root == null)
        {
            Debug.LogWarning(
                "[ScreenVignetteCommand] Failed to resolve PresentationUIRoot.");
            return;
        }

        _controller = root.GetScreenVignetteEffect();

        if (_controller != null)
            return;

        Debug.LogWarning(
            "[ScreenVignetteCommand] Failed to resolve ScreenVignetteEffectController.",
            root);
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
        switch (preset)
        {
            case ScreenVignettePreset.DefaultFocus:
                return new VignetteState(
                    0.35f * intensity,
                    Color.black,
                    0.25f,
                    0.10f,
                    1.2f);

            case ScreenVignettePreset.Tension:
                return new VignetteState(
                    0.55f * intensity,
                    Color.black,
                    0.15f,
                    0.22f,
                    1.2f);

            case ScreenVignettePreset.Horror:
                return new VignetteState(
                    0.78f * intensity,
                    new Color(0.02f, 0.015f, 0.018f, 1f),
                    0.14f,
                    0.36f,
                    1.2f);

            case ScreenVignettePreset.Danger:
                return new VignetteState(
                    0.58f * intensity,
                    new Color(0.35f, 0.02f, 0.015f, 1f),
                    0.14f,
                    0.34f,
                    1.2f);

            case ScreenVignettePreset.Memory:
                return new VignetteState(
                    0.36f * intensity,
                    new Color(0.34f, 0.38f, 0.48f, 1f),
                    0.10f,
                    0.36f,
                    1.2f);

            case ScreenVignettePreset.Dream:
                return new VignetteState(
                    0.32f * intensity,
                    new Color(0.38f, 0.32f, 0.52f, 1f),
                    0.34f,
                    0.12f,
                    1.2f);

            default:
                return new VignetteState(
                    0.35f * intensity,
                    Color.black,
                    0.25f,
                    0.20f,
                    1.777f);
        }
    }

    private VignetteState BuildLetterBoxState(float amount, float intensity)
    {
        float t = Mathf.Clamp01(amount) * intensity;

        // SG structure:
        // Aspect = 0 removes horizontal distance.
        // Distance becomes mostly abs(uv.y - 0.5).
        // Higher radius means less visible bar.
        // Lower radius means bars move further inward.
        float radius = Mathf.Lerp(0.52f, 0.23f, t);

        return new VignetteState(
            1f,
            Color.black,
            radius,
            Mathf.Max(0.001f, _spec.letterBoxSoftness),
            0f);
    }

    private void CommitFinalState()
    {
        _tween?.Kill(false);

        if (_controller != null)
            ApplyState(_destState);

        ClearRuntimeRefs();
    }

    private void ApplyState(VignetteState state)
    {
        if (_controller == null)
            return;

        _controller.ApplyImmediate(
            state.Amount,
            state.Color,
            state.Radius,
            state.Softness,
            state.Aspect);
    }

    private void ClearRuntimeRefs()
    {
        _canApply = false;
        _controller = null;
        _tween = null;
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