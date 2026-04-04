public sealed class TransitionCommandFactory : INodeCommandFactory
{
    private readonly TransitionCoordinator _coordinator;
    private readonly UIPatchService _uiPatchService;

    public TransitionCommandFactory(TransitionCoordinator coordinator, UIPatchService uiPatchService)
    {
        _coordinator = coordinator;
        _uiPatchService = uiPatchService;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            TransitionCommandSpec s => new TransitionCommand(_coordinator, s),
            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            _ => null
        };

        return command != null;
    }
}