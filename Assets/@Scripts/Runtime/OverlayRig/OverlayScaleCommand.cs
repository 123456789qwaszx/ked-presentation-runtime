using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Scale",
    Order = -938)]
public sealed class OverlayScaleCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Target")]
    public OverlayRigTarget target = OverlayRigTarget.Overlay_Scale;

    [Header("Scale")]
    public bool relativeToCurrent = false;
    public Vector2 scale = Vector2.one;

    [Header("Tween")]
    public float duration = 0f;
    public Ease ease = Ease.OutCubic;
}

public sealed class OverlayScaleCommand : ClaimTweenCommandBase
{
    private readonly OverlayScaleCommandSpec _spec;

    private OverlayRigRefs _refs;
    private RectTransform _rect;

    private Vector3 _startScale;
    private Vector3 _destScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public OverlayScaleCommand(OverlayScaleCommandSpec spec)
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

        _startScale = _rect.localScale;

        Vector3 requested = new(
            _spec.scale.x,
            _spec.scale.y,
            1f);

        // 상대 배율은 z를 건드리지 않는다 — UI에서 z 배율은 의미가 없다.
        _destScale = _spec.relativeToCurrent
            ? new Vector3(
                _startScale.x * requested.x,
                _startScale.y * requested.y,
                _startScale.z)
            : requested;
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOScale(_destScale, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _refs.SetLocalScaleImmediate(_spec.target, _destScale);
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            Vector3.Distance(_startScale, _destScale),
            Vector3.Distance(_rect.localScale, _destScale));
}
