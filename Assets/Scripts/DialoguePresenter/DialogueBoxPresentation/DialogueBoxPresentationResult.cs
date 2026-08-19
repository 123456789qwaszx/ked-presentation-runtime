public sealed class DialogueBoxPresentationResult
{
    public IPresentationDialogueBoxView NextBox { get; }

    public DialogueBoxPresentationResult(IPresentationDialogueBoxView nextBox)
    {
        NextBox = nextBox;
    }
}