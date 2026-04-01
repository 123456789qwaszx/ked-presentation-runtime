using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Move By (XY)",
    Order = -200)]
public class MoveByCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target (Track or Rig)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Delta (relative offset)")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 dest로 스냅")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;

    [Tooltip("체크하면 트윈이 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = false;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끊고 시작합니다.")]
    public bool killTween = true;
}


public sealed class MoveByCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly MoveByCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;
    
    private bool _hasComputedDest;
    private Vector2 _startPos;
    private Vector2 _destPos;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public MoveByCommandCharR(MoveByCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(false);
        
        ComputeDestIfNeeded();

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = _destPos;
            yield break;
        }

        Tween tween = _rect
            .DOAnchorPos(_destPos, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true);
        
        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        OnCommandCompleted(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        if (_rect == null)
            return;
        
        _rect.DOKill();

        ComputeDestIfNeeded();
        _rect.anchoredPosition = _destPos;
        
        _rect = null;
    }
    
    
    private void ComputeDestIfNeeded()
    {
        if (_hasComputedDest)
            return;

        _hasComputedDest = true;
        _startPos = _rect.anchoredPosition;
        _destPos = _startPos + _spec.delta;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
}