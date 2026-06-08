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

    [Header("Options")]
    public bool killTween = true;
    public bool clearOnSkipOrRollback = true;
}

public sealed class ScreenFlashCommand : CommandBase
{
    private readonly ScreenFlashCommandSpec _spec;
    private readonly ScreenFlashPresetDBSO _presetDb;

    private ScreenFlashEffectController _controller;
    private Sequence _sequence;

    private bool _resolveAttempted;
    private bool _canApply;

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

        if (_spec.killTween)
            _controller.KillTween(false);

        _canApply = true;

        FlashSettings settings = BuildSettings();
        float targetAmount = settings.Amount;

        if (settings.AttackDuration <= 0f &&
            settings.HoldDuration <= 0f &&
            settings.ReleaseDuration <= 0f)
        {
            _controller.ApplyImmediate(0f, settings.Color);
            ClearRuntimeRefs();
            yield break;
        }

        _controller.ApplyImmediate(0f, settings.Color);

        _sequence = DOTween.Sequence()
            .SetTarget(_controller.transform)
            .SetUpdate(true);

        if (settings.AttackDuration > 0f)
        {
            _sequence.Append(DOTween.To(
                    () => _controller != null ? _controller.FlashAmount : 0f,
                    value =>
                    {
                        if (!_canApply || _controller == null)
                            return;

                        _controller.ApplyImmediate(value, settings.Color);
                    },
                    targetAmount,
                    settings.AttackDuration)
                .SetEase(settings.AttackEase));
        }
        else
        {
            _sequence.AppendCallback(() =>
            {
                if (!_canApply || _controller == null)
                    return;

                _controller.ApplyImmediate(targetAmount, settings.Color);
            });
        }

        if (settings.HoldDuration > 0f)
            _sequence.AppendInterval(settings.HoldDuration);

        if (settings.ReleaseDuration > 0f)
        {
            _sequence.Append(DOTween.To(
                    () => _controller != null ? _controller.FlashAmount : targetAmount,
                    value =>
                    {
                        if (!_canApply || _controller == null)
                            return;

                        _controller.ApplyImmediate(value, settings.Color);
                    },
                    0f,
                    settings.ReleaseDuration)
                .SetEase(settings.ReleaseEase));
        }
        else
        {
            _sequence.AppendCallback(() =>
            {
                if (!_canApply || _controller == null)
                    return;

                _controller.ApplyImmediate(0f, settings.Color);
            });
        }

        _sequence.OnComplete(() =>
        {
            if (!_canApply || _controller == null)
                return;

            _controller.ApplyImmediate(0f, settings.Color);
            ClearRuntimeRefs();
        });

        if (_spec.wait)
            yield return _sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_controller == null)
            return;

        _sequence?.Kill(false);
        _controller.KillTween(false);

        if (_spec.clearOnSkipOrRollback)
            _controller.ClearImmediate();

        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_controller == null)
            return;

        _sequence?.Kill(false);
        _controller.KillTween(false);
        _controller.ClearImmediate();

        ClearRuntimeRefs();
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

                // SO 미할당/누락 시 폴백 (기존 default white flash).
                return new FlashSettings(
                    Color.white, 1f * intensity, 0.02f, 0.01f, 0.16f, Ease.OutCubic, Ease.OutCubic);

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

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        PresentationUIRoot root = UIManager.Instance.GetUI<PresentationUIRoot>();

        if (root == null)
        {
            Debug.LogWarning("[ScreenFlashCommand] Failed to resolve PresentationUIRoot.");
            return;
        }

        _controller = root.GetScreenFlashEffect();

        if (_controller != null)
            return;

        Debug.LogWarning("[ScreenFlashCommand] Failed to resolve ScreenFlashEffectController.", root);
    }

    private void ClearRuntimeRefs()
    {
        _canApply = false;
        _controller = null;
        _sequence = null;
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