using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class StageLayerBlurCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Far;

    [Header("Blur")]
    [Tooltip("0=선명, 1=최대. 셰이더의 MaxLod를 0~2로 스케일한다.")]
    [Range(0f, 1f)] public float amount = 1f;

    [Header("Tween")]
    public float duration = 0.8f;
    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// 한 stage/layer 아래에 있는 리그들의 블러를 함께 움직인다.
///
/// 옛 캡처 방식(레이어를 통째로 RT에 굽고 오버레이로 덮기)을 대체한다. 판단은 그대로다 —
/// "이 레이어에 있는 것을 흐린다". 기구만 요소별 셰이더로 바뀌었고, 그래서
/// 착란원이 요소마다 따로 서고 렌더러 종류(Image / 훗날 SkeletonGraphic)를 가리지 않는다.
///
/// 레이어 아래의 리그를 전부 훑는다. 배경도 캐릭터 리그다(캐스팅하지 않은 슬롯) — far 레이어에는
/// 보통 배경만 있지만, 캐릭터를 그 레이어에 올리면 같이 흐려지는 것이 이 커맨드의 의미상 맞다.
///
/// 리그마다 "레이어 블러" 의도만 넘긴다 — 전환은 각 리그의 RigVisualEffectController가 소유하고,
/// 모양(char_visual)·초점(focus_on) 블러와는 가장 큰 값으로 합쳐진다.
/// </summary>
public sealed class StageLayerBlurCommand : CommandBase
{
    // 화면의 큰 면적이 한꺼번에 초점을 되찾으면 눈에 띄게 튄다 — 훨씬 완만하게 붙인다.
    private const float StepFinishSpeedUpMultiplier = 1.5f;

    private readonly StageLayerBlurCommandSpec _spec;
    private readonly IStageDepthContentSlotProvider _slots;
    private readonly PresentationStage _stage;

    private readonly List<RigVisualEffectController> _targets = new();
    private readonly List<CharacterRigRefs> _charScratch = new();

    public override bool WaitForCompletion => _spec.wait;

    public StageLayerBlurCommand(
        PresentationStage stage,
        StageLayerBlurCommandSpec spec,
        IStageDepthContentSlotProvider slots)
    {
        _stage = stage;
        _spec = spec;
        _slots = slots;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        CollectTargets();

        float amount = Mathf.Clamp01(_spec.amount);

        for (int i = 0; i < _targets.Count; i++)
            _targets[i].FollowLayerBlur(amount, _spec.duration, _spec.ease);

        if (!_spec.wait)
            yield break;

        while (AnyTransitioning())
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        CollectTargets();

        float amount = Mathf.Clamp01(_spec.amount);

        for (int i = 0; i < _targets.Count; i++)
            _targets[i].SnapLayerBlur(amount);
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        for (int i = 0; i < _targets.Count; i++)
            _targets[i].AccelerateLayerBlur(StepFinishSpeedUpMultiplier);
    }

    // 실행 시점에 레이어 아래에 있던 리그만 잡는다.
    // 뒤늦게 붙는 리그는 의도적으로 포함하지 않는다 (레거시 CollectEdgeHideControllers와 같은 규칙).
    private void CollectTargets()
    {
        _targets.Clear();

        RectTransform contentRoot = _slots?.GetDepthContent(_spec.stage, _spec.layer);

        if (contentRoot == null)
        {
            Debug.LogWarning(
                $"[StageLayerBlurCommand] Depth content slot is missing. " +
                $"stage='{_spec.stage}', layer='{_spec.layer}'.");
            return;
        }

        _charScratch.Clear();
        _stage.characterRigs.CollectAliveRigs(_charScratch);

        for (int i = 0; i < _charScratch.Count; i++)
        {
            CharacterRigRefs refs = _charScratch[i];

            if (refs?.VisualEffect == null)
                continue;

            if (!IsDescendantOf(refs.RigRoot, contentRoot))
                continue;

            _targets.Add(refs.VisualEffect);
        }
    }

    private bool AnyTransitioning()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i].IsLayerBlurTransitioning)
                return true;
        }

        return false;
    }

    private static bool IsDescendantOf(Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return false;

        Transform t = child;

        while (t != null)
        {
            if (t == parent)
                return true;

            t = t.parent;
        }

        return false;
    }
}
