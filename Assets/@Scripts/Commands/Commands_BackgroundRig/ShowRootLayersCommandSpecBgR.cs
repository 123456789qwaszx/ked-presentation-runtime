using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig",
    "#Show Root Layers",
    Order = -939
)]
public sealed class ShowRootLayersCommandSpecBgR : BackgroundRigCommandSpecBase
{
    public BackgroundRigRootMask targetMask = BackgroundRigRootMask.VisualLayers;

    [Header("Interaction")]
    [Tooltip("true면 보인 대상의 입력을 허용(interactable/blocksRaycasts=true)")]
    public bool enableInteraction = false;
}

public sealed class ShowRootLayersCommandBgR : CommandBase
{
    private readonly ShowRootLayersCommandSpecBgR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShowRootLayersCommandBgR(ShowRootLayersCommandSpecBgR spec)
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

        if (_spec.targetMask == BackgroundRigRootMask.None)
            return;

        BackgroundRigRefs rig =
            BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(
                scope,
                _spec.rigKey);

        BackgroundRigRootSelector.CollectRects(
            rig,
            _spec.targetMask,
            _targets);
    }

    private void Apply()
    {
        if (_targets.Count == 0)
            return;

        SnapOnTargets(_targets);
    }

    private void SnapOnTargets(List<RectTransform> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(targets[i]);

            canvasGroup.DOKill(true);
            canvasGroup.alpha = 1f;

            if (_spec.enableInteraction)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect.TryGetComponent(out CanvasGroup canvasGroup))
            return canvasGroup;

        Debug.LogWarning(
            $"[ShowRootLayersCommandBgR] CanvasGroup missing. Added automatically: {rect.name}",
            rect);

        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}