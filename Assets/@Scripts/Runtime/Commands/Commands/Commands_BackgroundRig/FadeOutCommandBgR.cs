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
    private const float StepFinishSpeedUpMultiplier = 30f;
    private const float TargetAlpha = 0f;

    private readonly FadeOutCommandSpecBgR _spec;

    private CanvasGroup _canvasGroup;

    private float _startAlpha;
    private Tween _tween;

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

        _tween = _canvasGroup
            .DOFade(TargetAlpha, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_canvasGroup)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
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

        scope.BackgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        RectTransform target = rig.GetRect(_spec.target);
        _canvasGroup = GetOrAddCanvasGroup(target);
    }

    private void ClaimTarget()
    {
        _canvasGroup.DOKill(true);

        _startAlpha = _canvasGroup.alpha;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _canvasGroup.alpha = TargetAlpha;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;
        
        _tween.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        _tween = _canvasGroup
            .DOFade(TargetAlpha, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_canvasGroup)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = Mathf.Abs(_startAlpha - TargetAlpha);
        float remainingDistance = Mathf.Abs(_canvasGroup.alpha - TargetAlpha);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect.TryGetComponent(out CanvasGroup group))
            return group;

        Debug.LogWarning($"[FadeOutCommandBgR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}