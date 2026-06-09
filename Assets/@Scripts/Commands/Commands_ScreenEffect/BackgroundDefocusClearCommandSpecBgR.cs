using System;
using System.Collections;
using UnityEngine;

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
    private readonly BackgroundDefocusClearCommandSpecBgR _spec;
    private readonly IBackgroundRigBlurRuntime _runtime;

    public override bool WaitForCompletion => _spec.wait;

    public BackgroundDefocusClearCommandBgR(
        BackgroundDefocusClearCommandSpecBgR spec,
        IBackgroundRigBlurRuntime runtime)
    {
        _runtime = runtime;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _runtime.HideDefocus(_spec.rigKey, _spec.duration);

        if (_spec.wait && _spec.duration > 0f)
            yield return new WaitForSeconds(_spec.duration);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        _runtime.HideDefocus(_spec.rigKey, 0f);
    }
}