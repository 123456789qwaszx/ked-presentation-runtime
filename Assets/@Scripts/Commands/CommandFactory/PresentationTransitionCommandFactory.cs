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
            
            // Daze
            FocusBlurCurtainCommandSpec s => new FocusBlurCurtainCommand(s),
            
            ClearAllTransitionsCommandSpec s => new ClearAllTransitionsCommand(),
            
            TransitionOutStripCommandSpec s => new TransitionOutStripCommand(s),
            TransitionOutSlantCommandSpec s => new TransitionOutSlantCommand(s),
            TransitionOutFocusCurtainCommandSpec s => new TransitionOutFocusCurtainCommand(s),

            _ => null
        };

        return command != null;
    }
}