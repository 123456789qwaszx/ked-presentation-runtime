public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly UIPatchService _uiPatchService;

    public PresentationControlCommandFactory(
        UIPatchService uiPatchService)
    {
        _uiPatchService = uiPatchService;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            _ => null
        };

        return command != null;
    }
}