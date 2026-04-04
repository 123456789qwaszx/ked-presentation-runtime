using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;


[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Fade",
    Order = 100
)]
public sealed class FadeCommandSpecCharR : CharRigCommandSpecBase
{
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Root;

    [Header("Fade")] [Range(0f, 1f)] [Tooltip("목표 알파 값 (0=완전 투명, 1=완전 불투명).")]
    public float toAlpha = 1f;

    [Tooltip("0 이상이면 이 값에서부터 페이드 시작, 음수면 현재 alpha에서 시작합니다.")]
    public float fromAlpha = 0.1f;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 toAlpha로 스냅합니다.")]
    public float duration = 0.8f;

    public Ease ease = Ease.OutCubic;

    public bool wait = false;
}

public sealed class FadeCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly FadeCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeCommandCharR(FadeCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;
        
        if (_spec.duration <= 0f)
        {
            SnapAlpha(_rect, _spec.toAlpha);
            yield break;
        }
        
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(_rect);
        
        canvasGroup.DOKill(false);

        if (_spec.fromAlpha >= 0f)
            canvasGroup.alpha = Mathf.Clamp01(_spec.fromAlpha);
        
        Tween tween = canvasGroup
            .DOFade(_spec.toAlpha, _spec.duration)
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

        SnapAlpha(_rect, _spec.toAlpha);
        _rect = null;
    }

    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
    
    private void SnapAlpha(RectTransform targets, float toAlpha)
    {
        RectTransform rect = targets;

        CanvasGroup group = GetOrAddCanvasGroup(rect);
        
        group.DOKill(false);

        group.alpha = toAlpha;
    }
    
    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect == null)
            return null;

        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        if (group != null)
            return group;

        Debug.LogWarning($"[CanvasFadeCommand] CanvasGroup missing. Added automatically: {rect.name}", rect);

        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}