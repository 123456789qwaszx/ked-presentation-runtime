public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly UIPatchService _uiPatchService;
    private readonly DialogueBoxHost _dialogueBoxResolver;
    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private readonly VNSideRunnerSyncHub _vnSideRunnerSyncHub;

    public PresentationControlCommandFactory(
        UIPatchService uiPatchService,
        DialogueBoxHost dialogueBoxResolver,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VNSideRunnerSyncHub vnSideRunnerSyncHub)
    {
        _uiPatchService = uiPatchService;
        _dialogueBoxResolver = dialogueBoxResolver;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _vnSideRunnerSyncHub = vnSideRunnerSyncHub;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            HideDialogueBoxCommandSpec s => new HideDialogueBoxCommand(s, _dialogueBoxResolver),

            SubPresentationAdvanceCommandSpec s => new SubPresentationAdvanceCommand(s, _vnSideRunnerSyncHub),

            _ => null
        };

        return command != null;
    }
}