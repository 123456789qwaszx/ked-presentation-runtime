public sealed class PresentationTransitionCommandFactory : INodeCommandFactory
{
    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s),

            // Strip 
            VerticalStripWipeCommandSpec s => new VerticalStripWipeCommand(s),
            
            // Daze
            FocusBlurCurtainCommandSpec s => new FocusBlurCurtainCommand(s),
            
            ClearAllTransitionsCommandSpec s => new ClearAllTransitionsCommand(),
            
            TransitionOutStripCommandSpec s => new TransitionOutStripCommand(s),
            TransitionOutFocusCurtainCommandSpec s => new TransitionOutFocusCurtainCommand(s),

            _ => null
        };

        return command != null;
    }
}