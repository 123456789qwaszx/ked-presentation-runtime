public sealed class BackgroundRigCommandFactory : INodeCommandFactory
{
    private readonly BackgroundRigSlotResolver _rigSlotResolver;
    private readonly BackgroundRigBuilder _rigBuilder;
    private readonly BackgroundSpriteResolver _spriteResolver;

    public BackgroundRigCommandFactory(
        BackgroundRigSlotResolver backgroundRigSlotResolver,
        BackgroundRigBuilder backgroundRigBuilder,
        BackgroundSpriteResolver backgroundSpriteResolver)
    {
        _rigSlotResolver = backgroundRigSlotResolver;
        _rigBuilder = backgroundRigBuilder;
        _spriteResolver = backgroundSpriteResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            SetupBackgroundRigCommandSpec s => new SetupBackgroundRigCommand(
                _rigSlotResolver,
                _rigBuilder,
                s),

            SetAnchorCommandSpecBgR s => new SetAnchorCommandBgR(s),

            SetBackgroundSpriteCommandSpecBgR s => new SetBackgroundSpriteCommandBgR(
                s,
                _spriteResolver),

            SetOriginSizeCommandSpecBgR s => new SetOriginSizeCommandBgR(s),

            FadeInCommandSpecBgR s => new FadeInCommandBgR(s),
            FadeOutCommandSpecBgR s => new FadeOutCommandBgR(s),

            _ => null
        };

        return command != null;
    }
}