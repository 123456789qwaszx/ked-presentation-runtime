public sealed class TransitionCommandFactory : INodeCommandFactory
{
    private readonly TransitionCoordinator _coordinator;

    public TransitionCommandFactory(TransitionCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            TransitionCommandSpec s => new TransitionCommand(_coordinator, s),
            _ => null
        };

        return command != null;
    }
}