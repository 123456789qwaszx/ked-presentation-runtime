using System;

// 무대 틀의 커맨드 — 컷인 창 · 그림 팝업, 그리고 틀 되돌리기(stage-frame.md).
public sealed class StageFrameCommandFactory : INodeCommandFactory
{
    private readonly StageFrameRegistry _frames;
    private readonly CutInPresetDBSO _cutInPreset;
    private readonly PopupPresetDBSO _popupPreset;

    // 실험 A — 틀 되돌리기가 열린 틀을 닫지 않는다(StageFrameResetCommand). 소유자가 정하면 지운다.
    private readonly bool _keepOpenFramesOnTx;

    public StageFrameCommandFactory(
        StageFrameRegistry frames,
        CutInPresetDBSO cutInPreset = null,
        PopupPresetDBSO popupPreset = null,
        bool keepOpenFramesOnTx = false)
    {
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));
        _keepOpenFramesOnTx = keepOpenFramesOnTx;

        // 프리셋 에셋이 없으면 코드 기본값(원본 게임 실측)으로 — 조용히 넘기지 않는다.
        if (cutInPreset == null)
        {
            UnityEngine.Debug.LogWarning(
                "[StageFrameCommandFactory] 컷인 프리셋(CutInPresetDBSO)이 연결되지 않았다. 코드 기본값으로 연다 — " +
                "Create › Ked › Presentation › Cut-in Preset으로 에셋을 만들어 조립에 연결하면 런타임에서 편집할 수 있다.");
            cutInPreset = CutInPresetDBSO.CreateDefault();
        }

        _cutInPreset = cutInPreset;

        if (popupPreset == null)
        {
            UnityEngine.Debug.LogWarning(
                "[StageFrameCommandFactory] 그림 팝업 프리셋(PopupPresetDBSO)이 연결되지 않았다. 코드 기본값(원본 실측)으로 연다 — " +
                "Create › Ked › Presentation › Popup Preset으로 에셋을 만들어 조립에 연결하면 런타임에서 편집할 수 있다.");
            popupPreset = PopupPresetDBSO.CreateDefault();
        }

        _popupPreset = popupPreset;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            // 컷인 — 덮는 무대의 창 · 판 · 마스크(docs/PLAN_cutin.md)
            CutInOpenCommandSpec s => new CutInOpenCommand(s, _cutInPreset, _frames),
            CutInCloseCommandSpec s => new CutInCloseCommand(s, _cutInPreset, _frames),

            // 그림 팝업 — 덮는 무대의 뒷막 + 그림(docs/PLAN_popup.md)
            PopupOpenCommandSpec s => new PopupOpenCommand(s, _popupPreset, _frames),
            PopupCloseCommandSpec s => new PopupCloseCommand(s, _popupPreset, _frames),

            // 틀 되돌리기 — tx · tx_stage_mask_clear가 마스크 커맨드 앞에 낸다(stage-frame.md §4)
            StageFrameResetCommandSpec s => new StageFrameResetCommand(s, _frames, _keepOpenFramesOnTx),

            _ => null
        };

        return command != null;
    }
}
