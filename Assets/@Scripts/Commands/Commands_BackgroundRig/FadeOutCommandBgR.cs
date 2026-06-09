using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig Motion",
    "Fade Out",
    Order = -815,
    Sets = new[]
    {
        CommandMenuSets.SetupBackground,
    },
    SetOrder = -815)]
public sealed class FadeOutCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Root;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class FadeOutCommandBgR : CommandBase
{
    private readonly FadeOutCommandSpecBgR _spec;

    private CanvasGroup _canvasGroup;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public FadeOutCommandBgR(FadeOutCommandSpecBgR spec)
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
            .DOFade(0f, _spec.duration)
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
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        HasClaimedTarget = false;
    }
    
    private static CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect.TryGetComponent(out CanvasGroup group))
            return group;

        Debug.LogWarning($"[FadeOutCommandBgR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}