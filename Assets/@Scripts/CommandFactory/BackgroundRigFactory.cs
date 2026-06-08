public sealed class BackgroundRigCommandFactory : INodeCommandFactory
{
    private readonly BackgroundRigSlotResolver _rigSlotResolver;
    private readonly BackgroundRigBuilder _rigBuilder;
    private readonly BackgroundSpriteResolver _spriteResolver;
    private readonly IBackgroundRigBlurRuntime _blurRuntime;

    public BackgroundRigCommandFactory(
        BackgroundRigSlotResolver backgroundRigSlotResolver,
        BackgroundRigBuilder backgroundRigBuilder,
        BackgroundSpriteResolver backgroundSpriteResolver,
        IBackgroundRigBlurRuntime blurRuntime)
    {
        _rigSlotResolver = backgroundRigSlotResolver;
        _rigBuilder = backgroundRigBuilder;
        _spriteResolver = backgroundSpriteResolver;
        _blurRuntime = blurRuntime;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Setup
            SetupBackgroundRigCommandSpec s => new SetupBackgroundRigCommand(s, _rigSlotResolver, _rigBuilder),

            // Basic State
            SetAnchorCommandSpecBgR s => new SetAnchorCommandBgR(s),
            SetBackgroundSpriteCommandSpecBgR s => new SetBackgroundSpriteCommandBgR(s, _spriteResolver),
            SetOriginSizeCommandSpecBgR s => new SetOriginSizeCommandBgR(s),

            // Visibility
            FadeInCommandSpecBgR s => new FadeInCommandBgR(s),
            FadeOutCommandSpecBgR s => new FadeOutCommandBgR(s),
            HideRootLayersCommandSpecBgR s => new HideRootLayersCommandBgR(s),
            ShowRootLayersCommandSpecBgR s => new ShowRootLayersCommandBgR(s),

            // Transform
            MoveByCommandSpecBgR s => new MoveByCommandBgR(s),
            ScaleToCommandSpecBgR s => new ScaleToCommandBgR(s),

            // Motion / Acting
            SlideInCommandSpecBgR s => new SlideInCommandBgR(s),
            SlideOutCommandSpecBgR s => new SlideOutCommandBgR(s),
            JoltCommandSpecBgR s => new JoltCommandBgR(s),
            TrembleCommandSpecBgR s => new TrembleCommandBgR(s),
            BreathInPlaceCommandSpecBgR s => new BreathInPlaceCommandBgR(s),

            // Defocus / Blur
            BackgroundDefocusCommandSpecBgR s => new BackgroundDefocusCommandBgR(s, _blurRuntime),
            BackgroundDefocusClearCommandSpecBgR s => new BackgroundDefocusClearCommandBgR(s, _blurRuntime),

            _ => null
        };

        return command != null;
    }
}