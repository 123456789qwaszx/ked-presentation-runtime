public interface IDialogueBoxViewResolver
{
    IDialogueTextTarget ResolveTarget(DialogueBoxKind kind);
    void ShowOnly(IDialogueTextTarget target);
    void HideAll();
}
public sealed class PresentationViewCommandFactory : INodeCommandFactory
{
    private readonly PresentationResponseRig _presentationResponseRig;
    private readonly IDialogueBoxViewResolver _dialogueBoxResolver;

    public PresentationViewCommandFactory(
        PresentationResponseRig presentationResponseRig,
        IDialogueBoxViewResolver dialogueBoxViewResolver)
    {
        _presentationResponseRig = presentationResponseRig;
        _dialogueBoxResolver = dialogueBoxViewResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Presentation View setup
            SetupPresentationViewCommandSpec s => new SetupPresentationViewCommand(_presentationResponseRig, s),
            
            RegisterBackgroundResponseBindingCommandSpec s => new RegisterBackgroundResponseBindingCommand(_presentationResponseRig, s),
            RegisterCharacterResponseBindingCommandSpec s => new RegisterCharacterResponseBindingCommand(_presentationResponseRig, s),
            

            SlantedMaskSlideInCommandSpec s => new SlantedMaskSlideInCommand(s),
            SlantedMaskSlideOutCommandSpec s => new SlantedMaskSlideOutCommand(s),
            
            VerticalStripWipeCommandSpec s => new VerticalStripWipeCommand(s),
            SlantedShutterCommandSpec s => new SlantedShutterCommand(s),
            FocusBlurFadeCommandSpec s => new FocusBlurFadeCommand(s),
            FocusBlurCurtainCommandSpec s => new FocusBlurCurtainCommand(s),
            
            // Presentation Shot / Response Rig
            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),
            
            ShotZoomFocusCommandSpec s => new ShotZoomFocusCommand(_presentationResponseRig, s),
            ShotToCommandSpec s => new ShotToCommand(_presentationResponseRig, s),
            
            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),
            
            SetCharRigCamFocusCommandSpec s => new SetCharRigCamFocusCommand(s),

            // Dialogue Box
            HideDialogueBoxCommandSpec s => new HideDialogueBoxCommand(s, _dialogueBoxResolver),

            _ => null
        };

        return command != null;
    }
}