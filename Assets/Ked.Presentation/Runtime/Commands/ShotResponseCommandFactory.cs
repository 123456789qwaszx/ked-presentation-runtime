using System;

public sealed class ShotResponseCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly PresentationShotResponseSystem _presentationResponseRig;
    private readonly CharacterFocusTuningDBSO _focusTuningDB;
    private readonly IShotResponseStageProvider _stageProvider;

    public ShotResponseCommandFactory(
        PresentationStage stage,
        PresentationShotResponseSystem presentationResponseRig,
        CharacterFocusTuningDBSO focusTuningDB,
        IShotResponseStageProvider stageProvider)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _presentationResponseRig = presentationResponseRig ?? throw new ArgumentNullException(nameof(presentationResponseRig));
        _focusTuningDB = focusTuningDB ? focusTuningDB : throw new ArgumentNullException(nameof(focusTuningDB));
        _stageProvider = stageProvider ?? throw new ArgumentNullException(nameof(stageProvider));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,
            
            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),

            ShotZoomFocusCommandSpec s => new ShotZoomFocusCommand(_stage, _presentationResponseRig, s, _focusTuningDB, _stageProvider),
            ShotToCommandSpec s => new ShotToCommand(_presentationResponseRig, s),

            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),

            _ => null
        };

        return command != null;
    }
}