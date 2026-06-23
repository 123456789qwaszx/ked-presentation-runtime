using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum ScreenNoiseMode
{
    Custom = 0,
    Preset = 1,
    Clear = 2
}

public enum ScreenNoisePreset
{
    Default = 0,
    Memory = 1,
    Horror = 2,
    Broadcast = 3,
    Dream = 4,
    RainMood = 5
}

[Serializable]
[CommandMenuHint(
    "Screen Effect",
    "Screen Noise",
    Order = -680,
    Sets = new[]
    {
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -680)]
public sealed class ScreenNoiseCommandSpec : CommandSpecBase
{
    [Header("Mode")]
    public ScreenNoiseMode mode = ScreenNoiseMode.Preset;
    public ScreenNoisePreset preset = ScreenNoisePreset.Default;

    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("Custom")]
    [Range(0f, 1f)] public float amount = 1f;
    public Color color = Color.white;
    [Min(0f)] public float scale = 0.8f;
    public float speedX = 0.015f;
    public float speedY = 0.012f;
    [Min(0f)] public float contrast = 1f;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;
}

public sealed class ScreenNoiseCommand : CommandBase
{
    private readonly ScreenNoiseCommandSpec _spec;
    private readonly ScreenNoisePresetDBSO _presetDb;

    private ScreenNoiseEffectController _controller;

    private NoiseState _fromState;
    private NoiseState _destState;

    private bool _resolveAttempted;

    private bool HasClaimedController { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public ScreenNoiseCommand(ScreenNoiseCommandSpec spec, ScreenNoisePresetDBSO presetDb)
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
                    NoiseState state = NoiseState.Lerp(_fromState, _destState, t);
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

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        PresentationUIRoot root = UIManager.Instance.GetUI<PresentationUIRoot>();
        _controller = root.GetScreenNoiseEffect();
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

    private NoiseState CaptureCurrentState()
    {
        return new NoiseState(
            _controller.Amount,
            _controller.Color,
            _controller.Scale,
            _controller.SpeedX,
            _controller.SpeedY,
            _controller.Contrast);
    }

    private NoiseState BuildDestState()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);

        switch (_spec.mode)
        {
            case ScreenNoiseMode.Custom:
                return new NoiseState(
                    _spec.amount * intensity,
                    _spec.color,
                    _spec.scale,
                    _spec.speedX,
                    _spec.speedY,
                    _spec.contrast);

            case ScreenNoiseMode.Preset:
                return BuildPresetState(_spec.preset, intensity);

            case ScreenNoiseMode.Clear:
                return new NoiseState(
                    0f,
                    _controller != null ? _controller.Color : Color.white,
                    _controller != null ? _controller.Scale : 0.8f,
                    _controller != null ? _controller.SpeedX : 0.015f,
                    _controller != null ? _controller.SpeedY : 0.012f,
                    _controller != null ? _controller.Contrast : 1f);

            default:
                return new NoiseState(0f, Color.white, 0.8f, 0.015f, 0.012f, 1f);
        }
    }

    private NoiseState BuildPresetState(ScreenNoisePreset preset, float intensity)
    {
        if (_presetDb != null && _presetDb.TryGet(preset, out ScreenNoisePresetDBSO.Entry e))
        {
            return new NoiseState(
                e.amount * intensity,
                e.color,
                e.scale,
                e.speedX,
                e.speedY,
                e.contrast);
        }

        return new NoiseState(1f * intensity, Color.white, 0.8f, 0.015f, 0.012f, 1f);
    }

    private void ApplyState(NoiseState state)
    {
        _controller.ApplyImmediate(
            state.Amount,
            state.Color,
            state.Scale,
            state.SpeedX,
            state.SpeedY,
            state.Contrast);
    }

    private readonly struct NoiseState
    {
        public readonly float Amount;
        public readonly Color Color;
        public readonly float Scale;
        public readonly float SpeedX;
        public readonly float SpeedY;
        public readonly float Contrast;

        public NoiseState(
            float amount,
            Color color,
            float scale,
            float speedX,
            float speedY,
            float contrast)
        {
            Amount = Mathf.Clamp01(amount);
            Color = color;
            Scale = Mathf.Max(0f, scale);
            SpeedX = speedX;
            SpeedY = speedY;
            Contrast = Mathf.Max(0f, contrast);
        }

        public static NoiseState Lerp(NoiseState from, NoiseState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new NoiseState(
                Mathf.Lerp(from.Amount, to.Amount, t),
                Color.Lerp(from.Color, to.Color, t),
                Mathf.Lerp(from.Scale, to.Scale, t),
                Mathf.Lerp(from.SpeedX, to.SpeedX, t),
                Mathf.Lerp(from.SpeedY, to.SpeedY, t),
                Mathf.Lerp(from.Contrast, to.Contrast, t));
        }
    }
}