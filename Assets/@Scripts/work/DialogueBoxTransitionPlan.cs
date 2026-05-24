public sealed class DialogueBoxTransitionPlan
{
    public DialogueBoxKind NextKind { get; private set; }
    public IDialogueTextTarget PreviousBox { get; private set; }
    public IDialogueTextTarget NextBox { get; private set; }
    public DialogueBoxTransitionKind TransitionKind { get; private set; }
    public bool UseImmediate { get; private set; }

    public DialogueBoxTransitionPlan(
        DialogueBoxKind nextKind,
        IDialogueTextTarget previousBox,
        IDialogueTextTarget nextBox,
        DialogueBoxTransitionKind transitionKind,
        bool useImmediate)
    {
        NextKind = nextKind;
        PreviousBox = previousBox;
        NextBox = nextBox;
        TransitionKind = transitionKind;
        UseImmediate = useImmediate;
    }
}