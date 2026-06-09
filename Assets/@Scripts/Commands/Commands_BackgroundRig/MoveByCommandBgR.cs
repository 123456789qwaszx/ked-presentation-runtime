using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig Motion",
    "Move By (XY)",
    Order = -200)]
public sealed class MoveByCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Track;

    [Header("Delta (relative offset)")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 dest로 스냅")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class MoveByCommandBgR : CommandBase
{
    private readonly MoveByCommandSpecBgR _spec;

    private RectTransform _rect;
    private Vector2 _startPos;
    private Vector2 _destPos;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public MoveByCommandBgR(MoveByCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = _rect
            .DOAnchorPos(_destPos, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rig = BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);

        _startPos = _rect.anchoredPosition;
        _destPos = _startPos + _spec.delta;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _destPos;

        HasClaimedTarget = false;
    }
}