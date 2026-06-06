public sealed class PresentationTransitionCommandFactory : INodeCommandFactory
{
    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Slanted Mask
            SlantedMaskSlideInCommandSpec s => new SlantedMaskSlideInCommand(s),
            SlantedMaskSlideOutCommandSpec s => new SlantedMaskSlideOutCommand(s),

            // Strip 
            VerticalStripWipeCommandSpec s => new VerticalStripWipeCommand(s),
            
            // Shutter
            SlantedShutterCommandSpec s => new SlantedShutterCommand(s),

            // Focus 
            FocusBlurFadeCommandSpec s => new FocusBlurFadeCommand(s),
            
            // Daze
            FocusBlurCurtainCommandSpec s => new FocusBlurCurtainCommand(s),
            
            ClearAllTransitionsCommandSpec s => new ClearAllTransitionsCommand(s),
            RevealWithTransitionCommandSpec s => new RevealWithTransitionCommand(s),
            
            TransitionOutShutterCommandSpec s => new TransitionOutShutterCommand(s),
            TransitionOutStripCommandSpec s => new TransitionOutStripCommand(s),
            TransitionOutSlantCommandSpec s => new TransitionOutSlantCommand(s),
            TransitionOutFocusFadeCommandSpec s => new TransitionOutFocusFadeCommand(s),
            TransitionOutFocusCurtainCommandSpec s => new TransitionOutFocusCurtainCommand(s),

            _ => null
        };

        return command != null;
    }
}