using TMPro;

public sealed class DialogueBoxPresentationResult
{
    public DialogueBoxTransitionPlan Plan { get; private set; }
    public IDialogueTextTarget Box { get; private set; }
    public TMP_Text LineText { get; private set; }
    public TMP_Text NameText { get; private set; }
    public bool IsStale { get; private set; }

    public DialogueBoxPresentationResult(DialogueBoxTransitionPlan plan, bool isStale)
    {
        Plan = plan;
        IsStale = isStale;

        Box = plan?.NextBox;

        LineText = Box?.LineText;
        NameText = Box?.NameText;
        
    }
}