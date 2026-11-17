using System;

public sealed class ShotResponseCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly PresentationShotResponseSystem _presentationResponseRig;
    private readonly CharacterFocusTuningDBSO _focusTuningDB;

    public ShotResponseCommandFactory(
        PresentationStage stage,
        PresentationShotResponseSystem presentationResponseRig,
        CharacterFocusTuningDBSO focusTuningDB)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _presentationResponseRig = presentationResponseRig ?? throw new ArgumentNullException(nameof(presentationResponseRig));
        _focusTuningDB = focusTuningDB ? focusTuningDB : throw new ArgumentNullException(nameof(focusTuningDB));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,
            
            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),

            // 묶음 — 줌 + 팬
            ShotFocusToCommandSpec s => new ShotFocusToCommand(_stage, _presentationResponseRig, s, _focusTuningDB),
            ShotToCommandSpec s => new ShotToCommand(_presentationResponseRig, s),

            // 축마다 하나 — 렌즈 · 헤드 · 몸
            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotPanCommandSpec s => new ShotPanCommand(_presentationResponseRig, s),
            ShotRollCommandSpec s => new ShotRollCommand(_presentationResponseRig, s),
            ShotDollyCommandSpec s => new ShotDollyCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),

            _ => null
        };

        return command != null;
    }
}