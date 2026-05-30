using System;
using TMPro;
using Yarn.Unity;

public enum DialogueBoxTransitionKind
{
    Keep = 0,
    Cut = 1,
    FadeIn = 2,
    FadeOutIn = 3,
    Hide = 4
}

public sealed class DialogueBoxPresentationOptions
{
    public bool IsSeekTargetLine { get; set; }
    public bool UseImmediateTransition { get; set; }
    public LinePresentationRun Run { get; set; }
}

public sealed class DialogueBoxPresentationResult
{
    public DialogueBoxTransitionPlan Plan { get; private set; }
    public IDialogueTextTarget Box { get; private set; }
    public TMP_Text LineText { get; private set; }
    public TMP_Text NameText { get; private set; }
    public bool IsStale { get; private set; }

    private DialogueBoxPresentationResult(DialogueBoxTransitionPlan plan, bool isStale)
    {
        Plan = plan;
        IsStale = isStale;

        Box = plan != null ? plan.NextBox : null;
        LineText = Box != null ? Box.LineText : null;
        NameText = Box != null ? Box.NameText : null;
    }

    public static DialogueBoxPresentationResult Completed(DialogueBoxTransitionPlan plan)
    {
        return new DialogueBoxPresentationResult(plan, false);
    }

    public static DialogueBoxPresentationResult Stale(DialogueBoxTransitionPlan plan)
    {
        return new DialogueBoxPresentationResult(plan, true);
    }
}

public sealed class VNDialogueLine
{
    public string TextId { get; private set; }
    public string Text { get; private set; }
    public string CharacterName { get; private set; }
    public bool HasCharacterName { get; private set; }
    public string[] Metadata { get; private set; }

    public VNDialogueLine(
        string textId,
        string text,
        string characterName,
        string[] metadata)
    {
        TextId = textId ?? string.Empty;
        Text = text ?? string.Empty;
        CharacterName = characterName ?? string.Empty;
        Metadata = metadata ?? Array.Empty<string>();

        HasCharacterName = !string.IsNullOrWhiteSpace(CharacterName);
    }

    public static VNDialogueLine FromLocalizedLine(LocalizedLine line)
    {
        if (line == null)
        {
            return new VNDialogueLine(
                string.Empty,
                string.Empty,
                string.Empty,
                null);
        }

        return new VNDialogueLine(
            line.TextID,
            line.TextWithoutCharacterName.Text,
            line.CharacterName,
            line.Metadata);
    }
}

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