using System;
using UnityEngine;

public enum DialogueBoxSlotKind
{
    Narration,
    Named,
}

[Serializable]
public sealed class DialogueBoxRouteState
{
    [SerializeField] private DialogueBoxKind _narrationBox = DialogueBoxKind.OnlyText;
    [SerializeField] private DialogueBoxKind _namedBox = DialogueBoxKind.NoPortrait;

    public DialogueBoxKind Resolve(bool hasCharacterName)
    {
        return hasCharacterName ? _namedBox : _narrationBox;
    }

    public DialogueBoxKind NarrationBox => _narrationBox;
    public DialogueBoxKind NamedBox => _namedBox;

    public void SetNarrationBox(DialogueBoxKind kind)
    {
        _narrationBox = kind;
    }

    public void SetNamedBox(DialogueBoxKind kind)
    {
        _namedBox = kind;
    }
}