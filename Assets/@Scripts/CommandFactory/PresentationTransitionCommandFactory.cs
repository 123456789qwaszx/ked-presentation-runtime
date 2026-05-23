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

            _ => null
        };

        return command != null;
    }
}