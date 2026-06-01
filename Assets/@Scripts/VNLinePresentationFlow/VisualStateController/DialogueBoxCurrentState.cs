public sealed class DialogueBoxCurrentState
{
    public DialogueBoxKind? BoxKind { get; private set; }
    public IDialogueTextTarget Box { get; private set; } = null;
    public bool IsVisible { get; private set; }

    public void Commit(DialogueBoxKind kind, IDialogueTextTarget box, DialogueBoxTransitionKind transitionKind)
    {
        BoxKind = kind;
        Box = box;
        IsVisible = transitionKind != DialogueBoxTransitionKind.Hide;
    }

    public void Reset()
    {
        BoxKind = null;
        Box = null;
        IsVisible = false;
    }
}