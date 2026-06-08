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