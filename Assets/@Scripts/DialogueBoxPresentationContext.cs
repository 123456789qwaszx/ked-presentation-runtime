using System;
using Yarn.Unity;

public sealed class DialogueBoxPresentationContext
{
    public LocalizedLine Line { get; }
    public LinePresentationRun Run { get; }

    public bool IsSeekTargetLine { get; }
    public bool UseImmediateTransition { get; }

    public string Text { get; }
    public string CharacterName { get; }
    public bool HasCharacterName { get; }
    public string[] Metadata { get; }

    public DialogueBoxPresentationContext(
        LocalizedLine line,
        LinePresentationRun run,
        bool isSeekTargetLine,
        bool useImmediateTransition)
    {
        Line = line;
        Run = run;
        IsSeekTargetLine = isSeekTargetLine;
        UseImmediateTransition = useImmediateTransition;

        if (line == null)
        {
            Text = string.Empty;
            CharacterName = string.Empty;
            HasCharacterName = false;
            Metadata = Array.Empty<string>();
            return;
        }

        Text = line.TextWithoutCharacterName.Text ?? string.Empty;
        CharacterName = line.CharacterName ?? string.Empty;
        HasCharacterName = !string.IsNullOrWhiteSpace(CharacterName);
        Metadata = line.Metadata ?? Array.Empty<string>();
    }
}