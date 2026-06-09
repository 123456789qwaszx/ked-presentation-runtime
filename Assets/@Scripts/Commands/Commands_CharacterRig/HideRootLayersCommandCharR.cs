using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "#Hide Root Layers",
    Order = -940
)]
public sealed class HideRootLayersCommandSpecCharR : CharacterRigCommandSpecBase
{
    public CharRigRootMask targetMask = CharRigRootMask.CharacterPortraitOverlay_Root
                                             | CharRigRootMask.CharacterEmoji_Root
                                             | CharRigRootMask.CharacterPortrait_Root;
}

public sealed class HideRootLayersCommandCharR : CommandBase
{
    private readonly HideRootLayersCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public HideRootLayersCommandCharR(HideRootLayersCommandSpecCharR spec)
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

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
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
            
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}