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

    [Header("Interaction")]
    [Tooltip("true면 숨긴 대상의 입력을 완전히 차단(interactable/blocksRaycasts=false)")]
    public bool disableInteraction = true;
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
        _targets.Clear();

        if (_spec.targetMask == CharRigRootMask.None)
            return;

        CharacterRigRefs rig =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.targetKey);

        CharRigRootSelector.CollectRootRects(
            rig,
            _spec.targetMask,
            _targets);
    }

    private void Apply()
    {
        if (_targets.Count == 0)
            return;

        SnapOffTargets(_targets);
    }

    private void SnapOffTargets(List<RectTransform> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(targets[i]);

            canvasGroup.DOKill(true);
            canvasGroup.alpha = 0f;

            if (_spec.disableInteraction)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect.TryGetComponent(out CanvasGroup canvasGroup))
            return canvasGroup;

        Debug.LogWarning(
            $"[HideRootLayersCommandCharR] CanvasGroup missing. Added automatically: {rect.name}",
            rect);

        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}