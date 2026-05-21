public sealed class BackgroundRigCommandFactory : INodeCommandFactory
{
    private readonly BackgroundRigSlotResolver _rigSlotResolver;
    private readonly BackgroundRigBuilder _rigBuilder;

    public BackgroundRigCommandFactory(
        BackgroundRigSlotResolver backgroundRigSlotResolver,
        BackgroundRigBuilder backgroundRigBuilder)
    {
        _rigSlotResolver = backgroundRigSlotResolver;
        _rigBuilder = backgroundRigBuilder;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            SetupBackgroundRigCommandSpec s => new SetupBackgroundRigCommand(_rigSlotResolver, _rigBuilder, s),

            SetAnchorCommandSpecBgR s => new SetAnchorCommandBgR(s),

            _ => null
        };

        return command != null;
    }
}