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
public class ShowRootLayersCommandSpecCharR : CharacterRigCommandSpecBase
{
    public CharRigRootMask targetMask = CharRigRootMask.CharacterPortrait_Root;
}

public sealed class ShowRootLayersCommandCharR : CommandBase
{
    private readonly ShowRootLayersCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    public ShowRootLayersCommandCharR(ShowRootLayersCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        CharRigRootSelector.CollectRects(rig, _spec.targetMask, _targets);
    }

    private void Apply()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            CanvasGroup canvasGroup = _targets[i].GetComponent<CanvasGroup>();

            canvasGroup.DOKill(true);
            
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}