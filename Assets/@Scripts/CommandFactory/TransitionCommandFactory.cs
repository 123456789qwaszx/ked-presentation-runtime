public sealed class TransitionCommandFactory : INodeCommandFactory
{
    private readonly UIPatchService _uiPatchService;

    public TransitionCommandFactory(
        UIPatchService uiPatchService)
    {
        _uiPatchService = uiPatchService;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            _ => null
        };

        return command != null;
    }
}