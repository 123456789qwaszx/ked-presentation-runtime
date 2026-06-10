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
    private readonly BackgroundDefocusCommandSpecBgR _spec;
    private readonly IBackgroundRigBlurRuntime _runtime;

    public override bool WaitForCompletion => _spec.wait;

    public BackgroundDefocusCommandBgR(
        BackgroundDefocusCommandSpecBgR spec,
        IBackgroundRigBlurRuntime runtime)
    {
        _spec = spec;
        _runtime = runtime;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope, _spec.duration);

        if (_spec.wait && _spec.duration > 0f)
            yield return new WaitForSeconds(_spec.duration);
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope, 0f);
    
    private void Apply(CommandRunScope scope, float duration)
    {
        scope.backgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs refs);

        _runtime.ShowDefocus(
            _spec.rigKey,
            refs,
            _spec.alpha,
            duration,
            _spec.blurRadius,
            _spec.iterations,
            _spec.downsample);
    }
}