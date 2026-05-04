using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Fade Out By Character",
    Order = -814
)]
public sealed class FadeOutByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    public CharRigRootLayerMask targetMask = CharRigRootLayerMask.CharacterPortrait_Root
                                             | CharRigRootLayerMask.CharacterPortraitOverlay_Root
                                             | CharRigRootLayerMask.CharacterEmoji_Root;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.38f;

    public Ease ease = Ease.OutCubic;

    [Tooltip("true면 숨긴 대상의 입력을 완전히 차단(interactable/blocksRaycasts=false)")]
    public bool disableInteraction = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class FadeOutByCharacterCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly FadeOutByCharacterCommandSpecCharR _spec;

    private readonly List<RectTransform> _targets = new();
    private bool _resolveAttempted;
    private int _pending;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeOutByCharacterCommandCharR(FadeOutByCharacterCommandSpecCharR spec) => _spec = spec;

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

                    if (cg == null)
                    {
                        if (_pending == 0)
                            _canCommitFinalState = false;
                        return;
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

        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[FadeOutByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[FadeOutByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[FadeOutByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        CharRigRootLayerMaskMap.CollectRects(rig, _spec.targetMask, _targets);
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

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}