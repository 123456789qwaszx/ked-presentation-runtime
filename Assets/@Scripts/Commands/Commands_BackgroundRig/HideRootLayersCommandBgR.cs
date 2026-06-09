using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig",
    "#Hide Root Layers",
    Order = -940
)]
public sealed class HideRootLayersCommandSpecBgR : BackgroundRigCommandSpecBase
{
    public BackgroundRigRootMask targetMask = BackgroundRigRootMask.VisualLayers;
}

public sealed class HideRootLayersCommandBgR : CommandBase
{
    private readonly HideRootLayersCommandSpecBgR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    public HideRootLayersCommandBgR(HideRootLayersCommandSpecBgR spec)
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

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        BackgroundRigRefs rig = BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);
        BackgroundRigRootSelector.CollectRects(rig, _spec.targetMask, _targets);
    }

    private void Apply()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            CanvasGroup canvasGroup = _targets[i].GetComponent<CanvasGroup>();

            canvasGroup.DOKill(true);
            
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
        }
    }
}