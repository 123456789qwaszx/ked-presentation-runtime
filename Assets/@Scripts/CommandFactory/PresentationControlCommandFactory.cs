public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly UIPatchService _uiPatchService;
    private readonly IDialogueBoxViewResolver _dialogueBoxResolver;

    public PresentationControlCommandFactory(UIPatchService uiPatchService, IDialogueBoxViewResolver dialogueBoxResolver)
    {
        _uiPatchService = uiPatchService;
        _dialogueBoxResolver = dialogueBoxResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            HideDialogueBoxCommandSpec s => new HideDialogueBoxCommand(s, _dialogueBoxResolver),

            _ => null
        };

        return command != null;
    }
}