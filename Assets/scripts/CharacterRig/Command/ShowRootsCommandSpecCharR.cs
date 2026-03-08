using System;
using DG.Tweening;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Show Root Layers", Order = -950,
    Sets = new[]
    {
        CommandMenuSets.ResetChar,
    }, SetOrder = -975)]
public class ShowRootsCommandSpecCharR : CharRigCommandSpecBase
{
    public CharRigRootLayerMask targetMask = CharRigRootLayerMask.CharacterPortrait_Root;

    [Header("Interaction")]
    [Tooltip("상호작용을 켤지 여부")]
    public bool enableInteraction = true;
}

public sealed class ShowRootsCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ShowRootsCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShowRootsCommandCharR(ShowRootsCommandSpecCharR spec)
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
            canvasGroup.alpha = 1f;

            if (_spec.enableInteraction)
            {
                canvasGroup.interactable   = true;
                canvasGroup.blocksRaycasts = true;
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