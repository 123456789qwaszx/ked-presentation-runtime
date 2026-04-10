using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

public enum SlideFromCharR
{
    Left = 0,
    Right,
    Up,
    Down,
}

public enum ShakeAxisCharR
{
    X  = 0,
    Y  = 1,
    XY = 2,
}

[Serializable]
[CommandMenuHint(
    "Char Rig Motion", 
    "Slide In", 
    Order = -770)]
public class SlideInCommandSpecCharR : CommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide")]
    public SlideFromCharR from = SlideFromCharR.Left;

    [Tooltip("슬라이드 시작 위치를 얼마나 멀리에서 잡을지")]
    public float distance = 480f;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 도착 위치로 스냅.")]
    public float duration = 1.2f;

    public Ease ease = Ease.OutCubic;

    [Tooltip("체크하면 슬라이드가 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = false;
}

public sealed class SlideInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SlideInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _destPos;
    
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlideInCommandCharR(SlideInCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _rect.DOKill(false);
        
        Vector2 start = _destPos + GetOffset(_spec.from, _spec.distance);
        _rect.anchoredPosition = start;

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

        _rect.anchoredPosition = _destPos;
        
        _rect = null;
        _destPos = default;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
        _destPos = _rect.anchoredPosition;
    }
    
    private Vector2 GetOffset(SlideFromCharR from, float distance)
    {
        switch (from)
        {
            case SlideFromCharR.Right: return new Vector2(+distance, 0f);
            case SlideFromCharR.Up:    return new Vector2(0f, +distance);
            case SlideFromCharR.Down:  return new Vector2(0f, -distance);
            case SlideFromCharR.Left:
            default:                 return new Vector2(-distance, 0f);
        }
    }
}