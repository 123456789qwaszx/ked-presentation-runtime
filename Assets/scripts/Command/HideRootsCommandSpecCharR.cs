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
public sealed class HideRootsCommandSpecCharR : CommandSpecBase
{
    public CharRigRootLayerMask targetMask = CharRigRootLayerMask.CharacterPortraitOverlay_Root
                                             | CharRigRootLayerMask.CharacterEmoji_Root
                                             | CharRigRootLayerMask.CharacterPortrait_Root;

    [Header("Interaction")]
    [Tooltip("true면 숨긴 대상의 입력을 완전히 차단(interactable/blocksRaycasts=false)")]
    public bool disableInteraction = true;
}

public sealed class HideRootsCommandCharR : CommandBase
{
    private readonly HideRootsCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public HideRootsCommandCharR(HideRootsCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }
    
    private void Apply()
    {
        if (_targets.Count == 0)
            return;
        
        SnapOffTargets(_targets);
        
        _targets.Clear();
    }

    private void SnapOffTargets(List<RectTransform> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(targets[i]);

            canvasGroup.DOKill(true); // Finish previous motion so this command starts from a committed state.
            canvasGroup.alpha = 0f;

            if (_spec.disableInteraction)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        _targets.Clear();

        if (_spec.targetMask == CharRigRootLayerMask.None)
            return;

        CharacterRigRefs rig = (CharacterRigRefs)scope.Refs[_spec.roleKey];
        CharRigRootLayerMaskMap.CollectRects(rig, _spec.targetMask, _targets);
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup))
            return canvasGroup;

        Debug.LogWarning($"[HideRootsCommandCharR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}