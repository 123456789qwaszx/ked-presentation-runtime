using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig Visual",
    "Background Defocus",
    Order = -120)]
public sealed class BackgroundDefocusCommandSpecBgR : CommandSpecBase
{
    [Header("Target")]
    public string rigKey;

    [Header("Defocus")]
    [Range(0f, 1f)] public float alpha = 1f;
    [Range(0f, 8f)] public float blurRadius = 3f;
    [Range(1, 6)] public int iterations = 2;
    public UIStageBlurDownsample downsample = UIStageBlurDownsample.Quarter;

    [Header("Tween")]
    public float duration = 0.35f;
}

public sealed class BackgroundDefocusCommandBgR : CommandBase
{
    private readonly IBackgroundRigBlurRuntime _runtime;
    private readonly BackgroundDefocusCommandSpecBgR _spec;

    public override bool WaitForCompletion => _spec.wait;

    public BackgroundDefocusCommandBgR(
        IBackgroundRigBlurRuntime runtime,
        BackgroundDefocusCommandSpecBgR spec)
    {
        _runtime = runtime;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _runtime?.ShowDefocus(
            _spec.rigKey,
            _spec.alpha,
            _spec.duration,
            _spec.blurRadius,
            _spec.iterations,
            _spec.downsample);

        if (_spec.wait && _spec.duration > 0f)
            yield return new WaitForSeconds(_spec.duration);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        _runtime?.ShowDefocus(
            _spec.rigKey,
            _spec.alpha,
            0f,
            _spec.blurRadius,
            _spec.iterations,
            _spec.downsample);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }
}

[Serializable]
[CommandMenuHint(
    "Background Rig Visual",
    "Clear Background Defocus",
    Order = -119)]
public sealed class BackgroundDefocusClearCommandSpecBgR : CommandSpecBase
{
    [Header("Target")]
    public string rigKey;

    [Header("Tween")]
    public float duration = 0.25f;
}

public sealed class BackgroundDefocusClearCommandBgR : CommandBase
{
    private readonly IBackgroundRigBlurRuntime _runtime;
    private readonly BackgroundDefocusClearCommandSpecBgR _spec;

    public override bool WaitForCompletion => _spec.wait;

    public BackgroundDefocusClearCommandBgR(
        IBackgroundRigBlurRuntime runtime,
        BackgroundDefocusClearCommandSpecBgR spec)
    {
        _runtime = runtime;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _runtime?.HideDefocus(_spec.rigKey, _spec.duration);

        if (_spec.wait && _spec.duration > 0f)
            yield return new WaitForSeconds(_spec.duration);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        _runtime?.HideDefocus(_spec.rigKey, 0f);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }
}