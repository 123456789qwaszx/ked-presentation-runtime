using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Move",
    Order = -940)]
public sealed class OverlayMoveCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Target")]
    public OverlayRigTarget target = OverlayRigTarget.Overlay_Track;

    [Header("Move")]
    public bool useAbsolutePosition = true;
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    public float duration = 0f;
    public Ease ease = Ease.OutCubic;
}

public sealed class OverlayMoveCommand : ClaimTweenCommandBase
{
    private readonly OverlayMoveCommandSpec _spec;

    private OverlayRigRefs _refs;
    private RectTransform _rect;

    private Vector2 _startPos;
    private Vector2 _destPos;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public OverlayMoveCommand(OverlayMoveCommandSpec spec)
    {
        _spec = spec;
    }

    protected override float ResolvePlaybackDuration(CommandRunScope scope)
        => scope.ScalePresentationDuration(_spec.duration);

    protected override bool TryResolveTargets(CommandRunScope scope)
    {
        if (!scope.OverlayRigs.TryGet(_spec.rigKey, out _refs))
            return false;

        _rect = _refs.GetRect(_spec.target);

        return _rect != null;
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _refs.KillTween(_spec.target, true);

        _startPos = _rect.anchoredPosition;
        _destPos = _spec.useAbsolutePosition
            ? _spec.delta
            : _startPos + _spec.delta;
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOAnchorPos(_destPos, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _refs.SetAnchoredPositionImmediate(_spec.target, _destPos);
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            Vector2.Distance(_startPos, _destPos),
            Vector2.Distance(_rect.anchoredPosition, _destPos));
}
