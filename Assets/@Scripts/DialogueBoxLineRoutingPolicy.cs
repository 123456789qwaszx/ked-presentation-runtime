using System;
using Yarn.Unity;

public enum DialogueBoxKind
{
    Portrait = 0,
    Speaker = 1,
    LetterBox = 2,
    OnlyText = 3
}

[Serializable]
public sealed class DialogueBoxLineRoutingPolicy
{
    private const DialogueBoxKind DefaultProtagonistLineBoxKind = DialogueBoxKind.Portrait;
    private const DialogueBoxKind DefaultNamedLineBoxKind = DialogueBoxKind.Speaker;

    private DialogueBoxKind _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
    private DialogueBoxKind _namedLineBoxKind = DefaultNamedLineBoxKind;

    public DialogueBoxKind ResolveBoxKind(LocalizedLine line, out bool hasCharacterName)
    {
        hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;
        return ResolveBoxKind(hasCharacterName);
    }

    public DialogueBoxKind ResolveBoxKind(bool hasCharacterName)
    {
        return hasCharacterName
            ? _namedLineBoxKind
            : _protagonistLineBoxKind;
    }

    public void SetProtagonistLineBoxKind(DialogueBoxKind kind)
    {
        _protagonistLineBoxKind = kind;
    }

    public void SetNamedLineBoxKind(DialogueBoxKind kind)
    {
        _namedLineBoxKind = kind;
    }

    public void ResetToDefaults()
    {
        _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
        _namedLineBoxKind = DefaultNamedLineBoxKind;
    }
}