using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig Motion",
    "Fade In",
    Order = -820,
    Sets = new[]
    {
        CommandMenuSets.SetupBackground,
    },
    SetOrder = -820)]
public sealed class FadeInCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Root;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class FadeInCommandBgR : CommandBase
{
    private readonly FadeInCommandSpecBgR _spec;

    private CanvasGroup _canvasGroup;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public FadeInCommandBgR(FadeInCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = _canvasGroup
            .DOFade(1f, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_canvasGroup)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rig = BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);
        RectTransform target = rig.GetRect(_spec.target);
        _canvasGroup = GetOrAddCanvasGroup(target);
    }

    private void ClaimTarget()
    {
        _canvasGroup.DOKill(true);

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        HasClaimedTarget = false;
    }
    
    private static CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect.TryGetComponent(out CanvasGroup group))
            return group;

        Debug.LogWarning($"[FadeInCommandBgR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}