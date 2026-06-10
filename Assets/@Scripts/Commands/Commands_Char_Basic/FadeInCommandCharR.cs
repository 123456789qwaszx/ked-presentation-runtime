using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Fade In",
    Order = -820
)]
public class FadeInCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Root;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class FadeInCommandCharR : CommandBase
{
    private readonly FadeInCommandSpecCharR _spec;

    private CanvasGroup _canvasGroup;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public FadeInCommandCharR(FadeInCommandSpecCharR spec)
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

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
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

        Debug.LogWarning($"[FadeInCommandCharR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}