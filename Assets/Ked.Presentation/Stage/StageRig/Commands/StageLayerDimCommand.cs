using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class StageLayerDimCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = StageKeyParser.DefaultStage;

    [Tooltip("이 층의 판 — 먼 층부터 이 층까지 어두워진다. allLayers면 쓰지 않는다.")]
    public float layerDepth = StageLayerLabels.Far;

    [Tooltip("true면 그 무대의 가장 앞 층의 판 — 무대 전체(stage_dim all).")]
    public bool allLayers;

    [Tooltip("true면 모든 무대 · 층의 판을 걷는다(stage_dim_reset). stage · layerDepth · amount는 쓰지 않는다.")]
    public bool resetAll;

    [Header("Dim")]
    [Tooltip("판의 투명도 — 그 아래 밝기는 ×(1 − 세기). 0이면 걷는다.")]
    [Range(0f, 1f)] public float amount;

    [Header("Tween")]
    public float duration = 0.3f;
    public Ease ease = Ease.Linear;
}

// 어둡게 — 무대 리그의 층마다 선 어둡기 판(StageLayerDimController)의 세기를 쓴다(docs/PLAN_screen-cover.md §3.2).
// 커맨드는 의도만 넘기고 전환은 판이 갖는다 — 스텝 경계에 묶이지 않아 줄을 넘는다. 기다리지 않는다.
// 압축(빨리 넘기기 · 스텝 서두르기 · 시크)에서는 끝값을 바로 세운다(OnSkip).
public sealed class StageLayerDimCommand : CommandBase
{
    private readonly StageLayerDimCommandSpec _spec;
    private readonly StageRigRegistry _stages;
    private readonly List<StageLayerDimController> _targets = new();

    public StageLayerDimCommand(StageLayerDimCommandSpec spec, StageRigRegistry stages)
    {
        _spec = spec;
        _stages = stages;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        float amount = TargetAmount();

        foreach (StageLayerDimController dim in ResolveTargets())
            dim.Follow(amount, _spec.duration, _spec.ease);

        yield break;
    }

    protected override void OnSkip()
    {
        float amount = TargetAmount();

        foreach (StageLayerDimController dim in ResolveTargets())
            dim.Snap(amount);
    }

    private float TargetAmount() => _spec.resetAll ? 0f : Mathf.Clamp01(_spec.amount);

    // 판은 닿았을 때 고른다 — 그 사이에 선 층(EnsureLayer)도 받는다. 없는 무대는 레지스트리가 예외로 알린다.
    private List<StageLayerDimController> ResolveTargets()
    {
        _targets.Clear();

        if (_spec.resetAll)
        {
            IReadOnlyList<StageRigRefs> stages = _stages.All;

            for (int i = 0; i < stages.Count; i++)
            {
                IReadOnlyList<StageLayerRefs> layers = stages[i].Layers;

                for (int l = 0; l < layers.Count; l++)
                    _targets.Add(layers[l].Dim);
            }

            return _targets;
        }

        if (_spec.allLayers)
        {
            IReadOnlyList<StageLayerRefs> layers = _stages.Get(_spec.stage).Layers;

            if (layers.Count == 0)
            {
                Debug.LogWarning($"[StageLayerDimCommand] 무대 '{_spec.stage}'에 선 층이 없다 — 어둡게 할 것이 없다.");
                return _targets;
            }

            // 가장 앞 층의 판은 그 뒤의 층을 모두 가린다 — 무대 전체.
            _targets.Add(layers[layers.Count - 1].Dim);
            return _targets;
        }

        _targets.Add(_stages.EnsureLayer(_spec.stage, _spec.layerDepth).Dim);
        return _targets;
    }
}
