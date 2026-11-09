using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class StageMaskClearCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = "stage01";

    [Tooltip("true면 무대 전부(tx_stage_mask_clear). stage는 무시.")]
    public bool allStages;

    [Header("Clear")]
    public StageMaskClearMode mode = StageMaskClearMode.UnmaskedFullVisible;
    public bool hideEdge = true;
}

// 무대 마스크를 정해진 모양으로 바로 둔다 — 진행 중인 tx 모션은 슬롯이 끊는다.
public sealed class StageMaskClearCommand : CommandBase
{
    private readonly StageMaskClearCommandSpec _spec;
    private readonly StageRigRegistry _stages;
    private readonly StageFrameRegistry _frames;

    public override bool WaitForCompletion => false;

    public StageMaskClearCommand(
        StageMaskClearCommandSpec spec,
        StageRigRegistry stages,
        StageFrameRegistry frames)
    {
        _spec = spec;
        _stages = stages;
        _frames = frames;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip()
    {
        Apply();
    }

    private void Apply()
    {
        // 마스크를 지우면 틀도 끝난다 — 컷인 창 · 판과 그림 팝업을 중립으로(docs/PLAN_cutin.md §5 · PLAN_popup.md §6).
        if (!_spec.allStages)
        {
            _frames.Reset(_spec.stage);
            _stages.Get(_spec.stage).Mask.Clear(_spec.mode, _spec.hideEdge);
            return;
        }

        _frames.ResetAll();

        for (int i = 0; i < _stages.All.Count; i++)
            _stages.All[i].Mask.Clear(_spec.mode, _spec.hideEdge);
    }
}
