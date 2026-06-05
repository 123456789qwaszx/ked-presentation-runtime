public sealed class ShotResponseCommandFactory : INodeCommandFactory
{
    private readonly PresentationResponseRig _presentationResponseRig;
    private readonly CharacterFocusTuningDBSO _focusTuningDB;

    public ShotResponseCommandFactory(
        PresentationResponseRig presentationResponseRig,
        CharacterFocusTuningDBSO focusTuningDB)
    {
        _presentationResponseRig = presentationResponseRig;
        _focusTuningDB = focusTuningDB;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            RegisterBackgroundResponseBindingCommandSpec s => new RegisterBackgroundResponseBindingCommand(s, _presentationResponseRig),
            RegisterCharacterResponseBindingCommandSpec s => new RegisterCharacterResponseBindingCommand(s, _presentationResponseRig),

            RemoveBackgroundResponseBindingCommandSpec s => new RemoveBackgroundResponseBindingCommand(s, _presentationResponseRig),
            RemoveCharacterResponseBindingCommandSpec s => new RemoveCharacterResponseBindingCommand(s, _presentationResponseRig),

            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),

            ShotZoomFocusCommandSpec s => new ShotZoomFocusCommand(_presentationResponseRig, s, _focusTuningDB),
            ShotToCommandSpec s => new ShotToCommand(_presentationResponseRig, s),

            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),

            _ => null
        };

        return command != null;
    }
}