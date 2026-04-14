using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Fade In",
    Order = -820
)]
public class FadeInCommandSpecCharR : CommandSpecBase
{
    public CharRigRootLayerMask targetMask = CharRigRootLayerMask.CharacterPortrait_Root;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.47f;

    public Ease ease = Ease.OutCubic;

    [Tooltip("true면 대상의 입력 기능 해금(interactable/blocksRaycasts=true)")]
    public bool EnableInteraction = true;

    public bool wait = false;
}

public sealed class FadeInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly FadeInCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;
    private int _pending;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeInCommandCharR(FadeInCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        _pending = 0;
        _canCommitFinalState = true;

        if (_targets.Count == 0)
        {
            _canCommitFinalState = false;
            yield break;
        }

        if (_spec.duration <= 0f)
        {
            SnapOnTargets(_targets);
            _canCommitFinalState = false;
            yield break;
        }

        for (int i = 0; i < _targets.Count; i++)
        {
            RectTransform rect = _targets[i];
            if (rect == null)
                continue;

            CanvasGroup cg = GetOrAddCanvasGroup(rect);
            if (cg == null)
                continue;

            //rect.DOKill(true);
            cg.DOKill(true);

            _pending++;

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
                    _pending = Mathf.Max(0, _pending - 1);

                    if (!_canCommitFinalState)
                        return;

                    if (cg == null)
                    {
                        if (_pending == 0)
                            _canCommitFinalState = false;
                        return;
                    }

                    if (_spec.EnableInteraction)
                    {
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }

                    if (_pending == 0)
                        _canCommitFinalState = false;
                });
        }

        if (_pending == 0)
        {
            _canCommitFinalState = false;
            yield break;
        }

        if (!_spec.wait)
            yield break;

        while (_pending > 0)
        {
            int aliveCount = CountAliveTargets();
            if (aliveCount == 0)
            {
                _pending = 0;
                _canCommitFinalState = false;
                yield break;
            }

            yield return null;
        }
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_targets.Count == 0)
        {
            _pending = 0;
            _canCommitFinalState = false;
            return;
        }

        SnapOnTargets(_targets);

        _pending = 0;
        _canCommitFinalState = false;
    }
    
    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_targets.Count == 0)
            return;

        if (!_canCommitFinalState)
        {
            _pending = 0;
            return;
        }

        for (int i = 0; i < _targets.Count; i++)
        {
            RectTransform rect = _targets[i];
            if (rect == null)
                continue;

            CanvasGroup cg = GetOrAddCanvasGroup(rect);
            if (cg == null)
                continue;

            cg.DOKill(false);
        }

        SnapOnTargets(_targets);

        _pending = 0;
        _canCommitFinalState = false;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        _targets.Clear();

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig))
            return;

        CharRigRootLayerMaskMap.CollectRects(rig, _spec.targetMask, _targets);
    }

    private void SnapOnTargets(List<RectTransform> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform rect = targets[i];
            if (rect == null)
                continue;

            CanvasGroup cg = GetOrAddCanvasGroup(rect);
            if (cg == null)
                continue;

            cg.DOKill(false);
            cg.alpha = 1f;

            if (_spec.EnableInteraction)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }
    }

    private int CountAliveTargets()
    {
        int count = 0;

        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i] != null)
                count++;
        }

        return count;
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect == null)
            return null;

        if (rect.TryGetComponent<CanvasGroup>(out var group))
            return group;

        Debug.LogWarning($"[CanvasFadeCommand] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}