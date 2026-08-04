public sealed class BackgroundRigCommandFactory : INodeCommandFactory
{
    private readonly BackgroundRigBuilder _rigBuilder;
    private readonly BackgroundRigSlotResolver _slotResolver;

    public BackgroundRigCommandFactory(
        BackgroundRigBuilder backgroundRigBuilder,
        BackgroundRigSlotResolver backgroundRigSlotResolver)
    {
        _rigBuilder = backgroundRigBuilder;
        _slotResolver = backgroundRigSlotResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Setup
            SetupBackgroundRigCommandSpec s => new SetupBackgroundRigCommand(s, _rigBuilder, _slotResolver),

            // Basic State
            SetAnchorCommandSpecBgR s => new SetAnchorCommandBgR(s),
            SetBackgroundSpriteCommandSpecBgR s => new SetBackgroundSpriteCommandBgR(s),
            SetOriginSizeCommandSpecBgR s => new SetOriginSizeCommandBgR(s),

            // Visibility
            FadeInCommandSpecBgR s => new FadeInCommandBgR(s),
            FadeOutCommandSpecBgR s => new FadeOutCommandBgR(s),

            // Transform
            MoveByCommandSpecBgR s => new MoveByCommandBgR(s),
            ScaleToCommandSpecBgR s => new ScaleToCommandBgR(s),
            RotateToCommandSpecBgR s => new RotateToCommandBgR(s),
            RotateByCommandSpecBgR s => new RotateByCommandBgR(s),

            // Motion / Acting
            SlideInCommandSpecBgR s => new SlideInCommandBgR(s),
            SlideOutCommandSpecBgR s => new SlideOutCommandBgR(s),
            JoltCommandSpecBgR s => new JoltCommandBgR(s),
            TrembleCommandSpecBgR s => new TrembleCommandBgR(s),
            BreathInPlaceCommandSpecBgR s => new BreathInPlaceCommandBgR(s),

            _ => null
        };

        return command != null;
    }
}