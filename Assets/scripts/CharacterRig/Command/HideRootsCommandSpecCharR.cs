using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "#Hide Root Layers", Order = -940
    // ,
    // Sets = new[]
    // {
    //     CommandMenuSets.BuildChar,
    // }, 
    // SetOrder = -970
    )]
public sealed class HideRootsCommandSpecCharR : CharRigCommandSpecBase
{
    public CharRigRootLayerMask targetMask = CharRigRootLayerMask.CharacterPortraitOverlay_Root 
                                             | CharRigRootLayerMask.CharacterEmoji_Root
                                             | CharRigRootLayerMask.CharacterPortrait_Root;
    
    [Header("Interaction")]
    [Tooltip("true면 숨긴 대상의 입력을 완전히 차단(interactable/blocksRaycasts=false)")]
    public bool disableInteraction = true;
}

public sealed class HideRootsCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly HideRootsCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public HideRootsCommandCharR(HideRootsCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        if (_targets.Count == 0)
            yield break;
        
        SnapOffTargets(_targets);
        
        _targets.Clear();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        OnCommandCompleted(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        if (_targets.Count == 0)
            return;
        
        SnapOffTargets(_targets);
        
        _targets.Clear();
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        if (_spec.targetMask == CharRigRootLayerMask.None)
            return;
        
        CharRigRootLayerMaskMap.CollectRects((CharacterRigRefs)scope.Refs[_spec.roleKey], _spec.targetMask, _targets);
    }
    
    
    private void SnapOffTargets(List<RectTransform> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform rect = targets[i];
            if (rect == null)
                continue;

            CanvasGroup canvasGroup = GetOrAddCanvasGroup(rect);
            if (canvasGroup == null)
                continue;

            canvasGroup.DOKill(false);
            canvasGroup.alpha = 0f;

            if (_spec.disableInteraction)
            {
                canvasGroup.interactable   = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
    
    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect == null)
            return null;

        CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            return canvasGroup;

        Debug.LogWarning($"[HideCommandSpecCharR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}