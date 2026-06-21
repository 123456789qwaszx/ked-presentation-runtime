public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly UIPatchService _uiPatchService;
    private readonly VNSideRunnerSyncHub _vnSideRunnerSyncHub;

    public PresentationControlCommandFactory(
        UIPatchService uiPatchService,
        VNSideRunnerSyncHub vnSideRunnerSyncHub)
    {
        _uiPatchService = uiPatchService;
        _vnSideRunnerSyncHub = vnSideRunnerSyncHub;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            SubPresentationAdvanceCommandSpec s => new SubPresentationAdvanceCommand(_vnSideRunnerSyncHub, s),

            _ => null
        };

        return command != null;
    }
}