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
public class ShowRootLayersCommandSpecCharR : CommandSpecBase
{
    public CharRigRootLayerMask targetMask = CharRigRootLayerMask.CharacterPortrait_Root;

    [Header("Interaction")]
    [Tooltip("상호작용을 켤지 여부")]
    public bool enableInteraction = true;
}

public sealed class ShowRootLayersCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ShowRootLayersCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShowRootLayersCommandCharR(ShowRootLayersCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        Apply();
        yield break;
    }
    
    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    private void Apply()
    {
        if (_targets.Count == 0)
            return;
        
        SnapOnTargets(_targets);
        
        _targets.Clear();
    }
    
    private void SnapOnTargets(List<RectTransform> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(targets[i]);

            canvasGroup.DOKill(false);
            canvasGroup.alpha = 1f;

            if (_spec.enableInteraction)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            Debug.Log(canvasGroup.alpha);
        }
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        _targets.Clear();

        if (_spec.targetMask == CharRigRootLayerMask.None)
            return;

        CharRigRootLayerMaskMap.CollectRects((CharacterRigRefs)scope.Refs[_spec.roleKey], _spec.targetMask, _targets);
    }


    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect.TryGetComponent(out CanvasGroup canvasGroup))
            return canvasGroup;

        Debug.LogWarning($"[ShowRootsCommandCharR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}