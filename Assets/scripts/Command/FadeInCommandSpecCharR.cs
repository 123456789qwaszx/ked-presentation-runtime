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

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeInCommandCharR(FadeInCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        _pending = 0;

        if (_spec.duration <= 0f)
        {
            SnapOnTargets(_targets);
            yield break;
        }

        // 리스트를 건드리지 말고 pending만 관리
        for (int i = 0; i < _targets.Count; i++)
        {
            RectTransform rect = _targets[i];
            if (rect == null)
                continue;

            CanvasGroup cg = GetOrAddCanvasGroup(rect);
            if (cg == null)
                continue;

            cg.DOKill(false);

            _pending++;

            // rect/cg 캡처는 안전(인덱스 캡처 X)
            cg.DOFade(1f, _spec.duration)
              .SetEase(_spec.ease)
              .SetUpdate(true)
              .OnComplete(() =>
              {
                  _pending = Mathf.Max(0, _pending - 1);
              });
        }

        if (!_spec.wait)
            yield break;

        while (_pending > 0)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        // 아직 Resolve 안 됐을 수 있으니 보장
        if (!_resolveAttempted)
            ResolveRefs(scope);

        // wait=false로 넘어가도 “스킵/완료”에서 상태를 고정하고 싶다면 여기서 스냅
        if (_targets.Count == 0)
            return;

        // 진행중 트윈은 끊고 상태 스냅
        for (int i = 0; i < _targets.Count; i++)
        {
            RectTransform rect = _targets[i];
            if (rect == null) continue;

            CanvasGroup cg = GetOrAddCanvasGroup(rect);
            if (cg == null) continue;

            cg.DOKill(false);
        }

        SnapOnTargets(_targets);

        _pending = 0;
        _targets.Clear(); // 다음 실행 안전
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
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
            if (rect == null) continue;

            CanvasGroup cg = GetOrAddCanvasGroup(rect);
            if (cg == null) continue;

            cg.DOKill(false);
            cg.alpha = 1f;

            if (_spec.EnableInteraction)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        if (rect == null) return null;

        if (rect.TryGetComponent<CanvasGroup>(out var group))
            return group;

        Debug.LogWarning($"[CanvasFadeCommand] CanvasGroup missing. Added automatically: {rect.name}", rect);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}
