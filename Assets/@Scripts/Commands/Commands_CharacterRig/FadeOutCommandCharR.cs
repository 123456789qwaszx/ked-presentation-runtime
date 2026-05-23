using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Fade Out",
    Order = -815
)]
public class FadeOutCommandSpecCharR : CharacterRigCommandSpecBase
{
    public CharRigRootMask targetMask = CharRigRootMask.CharacterPortrait_Root
                                             | CharRigRootMask.CharacterPortraitOverlay_Root
                                             | CharRigRootMask.CharacterEmoji_Root;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.38f;

    public Ease ease = Ease.OutCubic;

    [Tooltip("true면 숨긴 대상의 입력을 완전히 차단(interactable/blocksRaycasts=false)")]
    public bool disableInteraction = true;
}

public sealed class FadeOutCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly FadeOutCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;
    private int _pending;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeOutCommandCharR(FadeOutCommandSpecCharR spec) => _spec = spec;

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
            SnapOffTargets(_targets);
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

            cg.DOKill(true);

            _pending++;

            cg.DOFade(0f, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(cg)
                .OnComplete(() =>
                {
                    _pending = Mathf.Max(0, _pending - 1);

                    if (!_canCommitFinalState)
                        return;

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

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_targets.Count == 0)
        {
            _pending = 0;
            _canCommitFinalState = false;
            return;
        }

        SnapOffTargets(_targets);

        _pending = 0;
        _canCommitFinalState = false;
    }

    
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
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
                    0f,
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

                    if (_spec.disableInteraction)
                    {
                        cg.interactable = false;
                        cg.blocksRaycasts = false;
                    }

                    if (_pending == 0)
                        _canCommitFinalState = false;
                });
        }

        SnapOffTargets(_targets);

        _pending = 0;
        _canCommitFinalState = false;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        _targets.Clear();

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        
        CharRigRootSelector.CollectRects(rigRefs, _spec.targetMask, _targets);
    }

    private void SnapOffTargets(List<RectTransform> targets)
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
            cg.alpha = 0f;

            if (_spec.disableInteraction)
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
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