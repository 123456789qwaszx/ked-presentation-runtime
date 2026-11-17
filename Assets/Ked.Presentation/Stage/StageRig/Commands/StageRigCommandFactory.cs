using System;

// 무대 리그의 커맨드 — 마스크(tx · tx_stage_mask_clear)와 층의 어둡기 판(screen_dim)(stage-rig.md).
// 틀(컷인 · 팝업)을 모른다 — tx가 틀을 끝내는 규칙은 파서가 틀 되돌리기 커맨드를 앞에 내어 드러낸다(StageFrameCommandFactory).
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
