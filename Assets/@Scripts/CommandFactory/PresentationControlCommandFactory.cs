using Yarn.Unity;

public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly UIPatchService _uiPatchService;
    private readonly IDialogueBoxViewResolver _dialogueBoxResolver;
    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private readonly DialogueRunner _subPresentationRunner;

    public PresentationControlCommandFactory(
        UIPatchService uiPatchService,
        IDialogueBoxViewResolver dialogueBoxResolver,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        DialogueRunner subPresentationRunner)
    {
        _uiPatchService = uiPatchService;
        _dialogueBoxResolver = dialogueBoxResolver;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _subPresentationRunner = subPresentationRunner;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            HideDialogueBoxCommandSpec s => new HideDialogueBoxCommand(s, _dialogueBoxResolver),

            SubPresentationStartCommandSpec s => new SubPresentationStartCommand(_subPresentationRunner, s),

            SubPresentationAdvanceCommandSpec s => new SubPresentationAdvanceCommand(_dialogueAdvanceDispatcher, s),

            _ => null
        };

        return command != null;
    }
}