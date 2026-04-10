using System;
using UnityEngine;
using System.Collections;
using DG.Tweening;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Move To (XY)",
    Order = -190)]
public class MoveToCommandSpecCharR : CommandSpecBase
{
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;
    public Vector2 toAnchoredPos;

    [Header("Tween")] [Tooltip("트윈 시간. <= 0이면 즉시 toPosition으로 스냅.")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;

    [Tooltip("체크하면 트윈이 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = false;

    [Header("Options")] [Tooltip("체크하면 기존 위치 관련 트윈을 끊고 시작합니다.")]
    public bool killTween = true;
}

public sealed class MoveToCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly MoveToCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public MoveToCommandCharR(MoveToCommandSpecCharR spec)
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

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = _spec.toAnchoredPos;
            yield break;
        }

        Tween tween = _rect
            .DOAnchorPos(_spec.toAnchoredPos, _spec.duration)
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

        _rect.anchoredPosition = _spec.toAnchoredPos;
        _rect = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
}