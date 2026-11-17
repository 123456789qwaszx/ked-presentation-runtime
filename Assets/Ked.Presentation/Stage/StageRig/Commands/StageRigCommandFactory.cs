using System;

// 무대 리그의 커맨드 — 마스크(stage_mask · stage_mask_reset)와 층의 어둡기 판(stage_dim)(stage-rig.md).
// 틀(컷인 창)을 모른다 — 마스크만 움직인다. 컷인 판 · 팝업 그림은 무대 안의 리그라 마스크가 함께 자른다(stage-frame.md §4).
public sealed class StageRigCommandFactory : INodeCommandFactory
{
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDb;
    private readonly StageRigRegistry _stages;

    public StageRigCommandFactory(
        StageMaskMotionPresetDBSO stageMaskMotionPresetDb,
        StageRigRegistry stages)
    {
        _stageMaskMotionPresetDb = stageMaskMotionPresetDb ? stageMaskMotionPresetDb : throw new ArgumentNullException(nameof(stageMaskMotionPresetDb));
        _stages = stages ?? throw new ArgumentNullException(nameof(stages));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            // 마스크 — 무대를 어디까지 보여 줄지(stage-rig.md §2.5)
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDb, _stages),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s, _stages),

            // 어둡게 — 층마다 선 어둡기 판(docs/PLAN_screen-cover.md §3.2)
            StageLayerDimCommandSpec s => new StageLayerDimCommand(s, _stages),

            _ => null
        };

        return command != null;
    }
}
