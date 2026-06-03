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
    public float duration = 0.47f;

    public Ease ease = Ease.OutCubic;

    [Tooltip("true면 대상의 입력 기능 해금(interactable/blocksRaycasts=true)")]
    public bool enableInteraction = false;
}

public sealed class FadeInCommandBgR : CommandBase
{
    private readonly FadeInCommandSpecBgR _spec;

    private RectTransform _target;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;
    private bool _pending;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeInCommandBgR(FadeInCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        _pending = false;
        _canCommitFinalState = true;

        if (_target == null)
        {
            _canCommitFinalState = false;
            yield break;
        }

        CanvasGroup cg = GetOrAddCanvasGroup(_target);
        if (cg == null)
        {
            _canCommitFinalState = false;
            yield break;
        }

        cg.DOKill(true);

        if (_spec.duration <= 0f)
        {
            SnapOn(cg);
            _canCommitFinalState = false;
            yield break;
        }

        _pending = true;

        DOTween.To(
                () => cg != null ? cg.alpha : 0f,
                x =>
                {
                    if (!_canCommitFinalState || cg == null)
                        return;

                    cg.alpha = x;
                },
                1f,
                _spec.duration
            )
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(cg)
            .OnComplete(() =>
            {
                _pending = false;

                if (!_canCommitFinalState)
                    return;

                if (cg == null)
                {
                    _canCommitFinalState = false;
                    return;
                }

                if (_spec.enableInteraction)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                _canCommitFinalState = false;
            });

        if (!_spec.wait)
            yield break;

        while (_pending)
        {
            if (_target == null)
            {
                _pending = false;
                _canCommitFinalState = false;
                yield break;
            }

            yield return null;
        }
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_target == null)
        {
            _pending = false;
            _canCommitFinalState = false;
            return;
        }

        CanvasGroup cg = GetOrAddCanvasGroup(_target);
        SnapOn(cg);

        _pending = false;
        _canCommitFinalState = false;
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_target == null)
            return;

        if (!_canCommitFinalState)
        {
            _pending = false;
            return;
        }

        CanvasGroup cg = GetOrAddCanvasGroup(_target);
        SnapOn(cg);

        _pending = false;
        _canCommitFinalState = false;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rigRefs =
            BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);

        _target = rigRefs.GetRect(_spec.target);
    }

    private void SnapOn(CanvasGroup cg)
    {
        if (cg == null)
            return;

        cg.DOKill(false);
        cg.alpha = 1f;

        if (_spec.enableInteraction)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect == null)
            return null;

        if (rect.TryGetComponent<CanvasGroup>(out CanvasGroup group))
            return group;

        Debug.LogWarning($"[FadeInCommandBgR] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}