using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum ScreenFlashMode
{
    Custom = 0,
    Preset = 1
}

public enum ScreenFlashPreset
{
    Default = 0,
    Soft = 1,
    Hit = 2,
    Camera = 3
}

[Serializable]
[CommandMenuHint(
    "Screen Effect",
    "Screen Flash",
    Order = -700,
    Sets = new[]
    {
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -700)]
public sealed class ScreenFlashCommandSpec : CommandSpecBase
{
    [Header("Mode")]
    public ScreenFlashMode mode = ScreenFlashMode.Preset;
    public ScreenFlashPreset preset = ScreenFlashPreset.Default;

    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("Custom - Flash")]
    public Color color = Color.white;

    [Range(0f, 1f)]
    public float amount = 1f;

    [Header("Custom - Timing")]
    [Min(0f)] public float attackDuration = 0.03f;
    [Min(0f)] public float holdDuration = 0.02f;
    [Min(0f)] public float releaseDuration = 0.14f;

    public Ease attackEase = Ease.OutCubic;
    public Ease releaseEase = Ease.OutCubic;
}

public sealed class ScreenFlashCommand : CommandBase
{
    private readonly ScreenFlashCommandSpec _spec;
    private readonly ScreenFlashPresetDBSO _presetDb;

    private ScreenFlashEffectController _controller;
    private FlashSettings _settings;

    private bool _resolveAttempted;

    private bool HasClaimedController { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ScreenFlashCommand(ScreenFlashCommandSpec spec, ScreenFlashPresetDBSO presetDb)
    {
        _spec = spec;
        _presetDb = presetDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_controller == null)
            yield break;

        ClaimController();

        if (_settings.AttackDuration <= 0f &&
            _settings.HoldDuration <= 0f &&
            _settings.ReleaseDuration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _controller.ApplyImmediate(0f, _settings.Color);

        Sequence sequence = DOTween.Sequence()
            .SetTarget(_controller.transform)
            .SetUpdate(true);

        if (_settings.AttackDuration > 0f)
        {
            sequence.Append(DOTween.To(
                    () => _controller != null ? _controller.FlashAmount : 0f,
                    value =>
                    {
                        if (_controller == null)
                            return;

                        _controller.ApplyImmediate(value, _settings.Color);
                    },
                    _settings.Amount,
                    _settings.AttackDuration)
                .SetEase(_settings.AttackEase)
                .SetTarget(_controller.transform));
        }
        else
        {
            sequence.AppendCallback(() =>
            {
                if (_controller == null)
                    return;

                _controller.ApplyImmediate(_settings.Amount, _settings.Color);
            });
        }

        if (_settings.HoldDuration > 0f)
            sequence.AppendInterval(_settings.HoldDuration);

        if (_settings.ReleaseDuration > 0f)
        {
            sequence.Append(DOTween.To(
                    () => _controller != null ? _controller.FlashAmount : _settings.Amount,
                    value =>
                    {
                        if (_controller == null)
                            return;

                        _controller.ApplyImmediate(value, _settings.Color);
                    },
                    0f,
                    _settings.ReleaseDuration)
                .SetEase(_settings.ReleaseEase)
                .SetTarget(_controller.transform));
        }
        else
        {
            sequence.AppendCallback(() =>
            {
                if (_controller == null)
                    return;

                _controller.ApplyImmediate(0f, _settings.Color);
            });
        }

        sequence.OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return sequence.WaitForCompletion();
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
        _controller = root.GetScreenFlashEffect();
    }

    private void ClaimController()
    {
        DOTween.Kill(_controller.transform, true);
        _controller.KillTween(true);

        _settings = BuildSettings();

        HasClaimedController = true;
    }

    private void CommitFinalState()
    {
        DOTween.Kill(_controller.transform, false);
        _controller.KillTween(false);
        
        _controller.ApplyImmediate(0f, _settings.Color);

        HasClaimedController = false;
    }

    private FlashSettings BuildSettings()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);

        switch (_spec.mode)
        {
            case ScreenFlashMode.Preset:
                if (_presetDb != null && _presetDb.TryGet(_spec.preset, out ScreenFlashPresetDBSO.Entry e))
                {
                    return new FlashSettings(
                        e.color,
                        e.amount * intensity,
                        e.attackDuration,
                        e.holdDuration,
                        e.releaseDuration,
                        e.attackEase,
                        e.releaseEase);
                }

                return new FlashSettings(
                    Color.white,
                    1f * intensity,
                    0.02f,
                    0.01f,
                    0.16f,
                    Ease.OutCubic,
                    Ease.OutCubic);

            case ScreenFlashMode.Custom:
            default:
                return new FlashSettings(
                    _spec.color,
                    _spec.amount * intensity,
                    _spec.attackDuration,
                    _spec.holdDuration,
                    _spec.releaseDuration,
                    _spec.attackEase,
                    _spec.releaseEase);
        }
    }

    private readonly struct FlashSettings
    {
        public readonly Color Color;
        public readonly float Amount;
        public readonly float AttackDuration;
        public readonly float HoldDuration;
        public readonly float ReleaseDuration;
        public readonly Ease AttackEase;
        public readonly Ease ReleaseEase;

        public FlashSettings(
            Color color,
            float amount,
            float attackDuration,
            float holdDuration,
            float releaseDuration,
            Ease attackEase,
            Ease releaseEase)
        {
            Color = color;
            Amount = Mathf.Clamp01(amount);
            AttackDuration = Mathf.Max(0f, attackDuration);
            HoldDuration = Mathf.Max(0f, holdDuration);
            ReleaseDuration = Mathf.Max(0f, releaseDuration);
            AttackEase = attackEase;
            ReleaseEase = releaseEase;
        }
    }
}