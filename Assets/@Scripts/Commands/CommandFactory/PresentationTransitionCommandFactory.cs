public sealed class PresentationTransitionCommandFactory : INodeCommandFactory
{
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDbSo;

    public PresentationTransitionCommandFactory(StageMaskMotionPresetDBSO stageMaskMotionPresetDbSo)
    {
        _stageMaskMotionPresetDbSo = stageMaskMotionPresetDbSo;
    }
    
    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDbSo),
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