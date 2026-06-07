using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

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
    [Header("Flash")]
    public Color color = Color.white;

    [Range(0f, 1f)]
    public float amount = 1f;

    [Header("Timing")]
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

    private ScreenFlashEffectController _controller;
    private Sequence _sequence;

    private bool _resolveAttempted;
    private bool _canApply;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ScreenFlashCommand(ScreenFlashCommandSpec spec)
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
            _controller.KillTween(false);

        _canApply = true;

        float targetAmount = Mathf.Clamp01(_spec.amount);

        if (_spec.attackDuration <= 0f &&
            _spec.holdDuration <= 0f &&
            _spec.releaseDuration <= 0f)
        {
            _controller.ApplyImmediate(0f, _spec.color);
            ClearRuntimeRefs();
            yield break;
        }

        _controller.ApplyImmediate(0f, _spec.color);

        _sequence = DOTween.Sequence()
            .SetTarget(_controller.transform)
            .SetUpdate(true);

        if (_spec.attackDuration > 0f)
        {
            _sequence.Append(DOTween.To(
                    () => _controller != null ? _controller.FlashAmount : 0f,
                    value =>
                    {
                        if (!_canApply || _controller == null)
                            return;

                        _controller.ApplyImmediate(value, _spec.color);
                    },
                    targetAmount,
                    _spec.attackDuration)
                .SetEase(_spec.attackEase));
        }
        else
        {
            _sequence.AppendCallback(() =>
            {
                if (!_canApply || _controller == null)
                    return;

                _controller.ApplyImmediate(targetAmount, _spec.color);
            });
        }

        if (_spec.holdDuration > 0f)
            _sequence.AppendInterval(_spec.holdDuration);

        if (_spec.releaseDuration > 0f)
        {
            _sequence.Append(DOTween.To(
                    () => _controller != null ? _controller.FlashAmount : targetAmount,
                    value =>
                    {
                        if (!_canApply || _controller == null)
                            return;

                        _controller.ApplyImmediate(value, _spec.color);
                    },
                    0f,
                    _spec.releaseDuration)
                .SetEase(_spec.releaseEase));
        }
        else
        {
            _sequence.AppendCallback(() =>
            {
                if (!_canApply || _controller == null)
                    return;

                _controller.ApplyImmediate(0f, _spec.color);
            });
        }

        _sequence.OnComplete(() =>
        {
            if (!_canApply || _controller == null)
                return;

            _controller.ApplyImmediate(0f, _spec.color);
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

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        PresentationUIRoot root = UIManager.Instance.GetUI<PresentationUIRoot>();

        if (root == null)
        {
            Debug.LogWarning(
                "[ScreenFlashCommand] Failed to resolve PresentationUIRoot.");
            return;
        }

        _controller = root.GetScreenFlashEffect();

        if (_controller != null)
            return;

        Debug.LogWarning(
            "[ScreenFlashCommand] Failed to resolve ScreenFlashEffectController.",
            root);
    }

    private void ClearRuntimeRefs()
    {
        _canApply = false;
        _controller = null;
        _sequence = null;
    }
}