using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Hide",
    Order = -936)]
public sealed class OverlayHideCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Fade")]
    public float duration = 0.15f;
    public Ease ease = Ease.OutCubic;
}

public sealed class OverlayHideCommand : OverlayRootFadeCommandBase
{
    private readonly OverlayHideCommandSpec _spec;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    protected override string RigKey => _spec.rigKey;
    protected override Ease FadeEase => _spec.ease;
    protected override float TargetAlpha => 0f;

    public OverlayHideCommand(OverlayHideCommandSpec spec)
    {
        _spec = spec;
    }

    protected override float ResolvePlaybackDuration(CommandRunScope scope)
        => scope.ScalePresentationDuration(_spec.duration);
}
