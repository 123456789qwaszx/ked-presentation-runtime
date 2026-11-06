using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public class MoveByCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Track or Rig)")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Track;

    [Header("Move")]
    [Tooltip(
        "false: delta를 현재 anchoredPosition 기준 상대 오프셋으로 사용.\n" +
        "true: delta를 목표 anchoredPosition 절대값으로 사용.")]
    public bool useAbsolutePosition = false;

    [Tooltip(
        "useAbsolutePosition=false: 현재 anchoredPosition에 더해질 오프셋.\n" +
        "useAbsolutePosition=true: 이동할 목표 anchoredPosition.")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 dest로 스냅")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class MoveByCommandCharR : ClaimTweenCommandBase
{
    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;
    private readonly MoveByCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector2 _startPos;
    private Vector2 _destPos;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public MoveByCommandCharR(PresentationStage stage, MoveByCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveTargets()
    {
        _rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _rect = _rig.GetRect(_spec.target);
    }

    protected override void ClaimTarget()
    {
        _rect.DOKill(true);
        _startPos = _rect.anchoredPosition;

        NodeTarget target = MoveByTarget.Reduce(_rect, _spec, _startPos);
        _rig.PlacementTargets.Reserve(_rect, target, Releases);
        _destPos = target.Value.XY.ToUnity();
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOAnchorPos(_destPos, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
        => _rect.anchoredPosition = _destPos;

}

// MoveBy의 Unity → Core 경계. 커맨드가 변환과 Args 조립을 들고 있지 않도록 여기서만 한다.
// 시작점은 인자로 받는다 — 안에서 rect를 다시 읽으면 시작점을 먼저 잡아야 한다는 순서가 숨는다.
static class MoveByTarget
{
    public static NodeTarget Reduce(RectTransform rect, MoveByCommandSpecCharR spec, Vector2 start)
        => MoveByReduction.Reduce(
            rect.name,
            new MoveByReduction.Args(spec.useAbsolutePosition, spec.delta.ToCore()),
            start.ToCore());
}
