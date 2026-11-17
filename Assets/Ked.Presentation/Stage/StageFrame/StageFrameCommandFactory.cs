using System;

// 무대 틀의 커맨드 — 컷인 창(stage-frame.md). 컷인 판 · 그림 팝업은 틀이 아니다 — 파서가 무대 안의 리그로 푼다.
public sealed class StageFrameCommandFactory : INodeCommandFactory
{
    private readonly StageFrameRegistry _frames;
    private readonly CutInPresetDBSO _cutInPreset;

    public StageFrameCommandFactory(
        StageFrameRegistry frames,
        CutInPresetDBSO cutInPreset = null)
    {
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));

        // 프리셋 에셋이 없으면 코드 기본값(원본 게임 실측)으로 — 조용히 넘기지 않는다.
        if (cutInPreset == null)
        {
            UnityEngine.Debug.LogWarning(
                "[StageFrameCommandFactory] 컷인 프리셋(CutInPresetDBSO)이 연결되지 않았다. 코드 기본값으로 연다 — " +
                "Create › Ked › Presentation › Cut-in Preset으로 에셋을 만들어 조립에 연결하면 런타임에서 편집할 수 있다.");
            cutInPreset = CutInPresetDBSO.CreateDefault();
        }

        _cutInPreset = cutInPreset;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            // 컷인 — 덮는 무대의 창 · 판 · 마스크(docs/PLAN_cutin.md)
            CutInOpenCommandSpec s => new CutInOpenCommand(s, _cutInPreset, _frames),
            CutInCloseCommandSpec s => new CutInCloseCommand(s, _cutInPreset, _frames),

            _ => null
        };

        return command != null;
    }
}
