using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Depth Visual",
    "Depth Defocus",
    Order = -120)]
public sealed class StageDepthDefocusCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Back;

    [Header("Mode")]
    public bool visible = true;

    [Header("Defocus")]
    [Range(0f, 1f)] public float alpha = 1f;
    [Range(0f, 8f)] public float blurRadius = 3f;
    [Range(1, 6)] public int iterations = 2;
    public UIStageBlurDownsample downsample = UIStageBlurDownsample.Quarter;

    [Header("Tween")]
    public float duration = 0.35f;
}

public sealed class StageDepthDefocusCommand : CommandBase
{
    private readonly StageDepthDefocusCommandSpec _spec;
    private readonly IStageDepthLayerBlurRuntime _runtime;

    public override bool WaitForCompletion => _spec.wait;

    public StageDepthDefocusCommand(
        StageDepthDefocusCommandSpec spec,
        IStageDepthLayerBlurRuntime runtime)
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

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply(scope, 0f);
    }

    private void Apply(CommandRunScope scope, float duration)
    {
        if (_spec.visible)
        {
            _runtime.ShowDefocus(
                scope,
                _spec.stage,
                _spec.layer,
                _spec.alpha,
                duration,
                _spec.blurRadius,
                _spec.iterations,
                _spec.downsample);
        }
        else
        {
            _runtime.HideDefocus(
                _spec.stage,
                _spec.layer,
                duration);
        }
    }
}