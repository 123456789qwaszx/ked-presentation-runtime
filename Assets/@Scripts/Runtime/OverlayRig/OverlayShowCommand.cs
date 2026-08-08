using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Show",
    Order = -937)]
public sealed class OverlayShowCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Fade")]
    public float duration = 0.15f;
    public Ease ease = Ease.OutCubic;
}

public sealed class OverlayShowCommand : OverlayRootFadeCommandBase
{
    private readonly OverlayShowCommandSpec _spec;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    protected override string RigKey => _spec.rigKey;
    protected override Ease FadeEase => _spec.ease;
    protected override float TargetAlpha => 1f;

    public OverlayShowCommand(OverlayShowCommandSpec spec)
    {
        _spec = spec;
    }

    protected override float ResolvePlaybackDuration(CommandRunScope scope)
        => scope.ScalePresentationDuration(_spec.duration);
}
