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
    [SerializeField] private DialogueUIRoot.DialogueBoxKind _narrationBox = DialogueUIRoot.DialogueBoxKind.OnlyText;
    [SerializeField] private DialogueUIRoot.DialogueBoxKind _namedBox = DialogueUIRoot.DialogueBoxKind.NoPortrait;

    public DialogueUIRoot.DialogueBoxKind Resolve(bool hasCharacterName)
    {
        return hasCharacterName ? _namedBox : _narrationBox;
    }

    public DialogueUIRoot.DialogueBoxKind NarrationBox => _narrationBox;
    public DialogueUIRoot.DialogueBoxKind NamedBox => _namedBox;

    public void SetNarrationBox(DialogueUIRoot.DialogueBoxKind kind)
    {
        _narrationBox = kind;
    }

    public void SetNamedBox(DialogueUIRoot.DialogueBoxKind kind)
    {
        _namedBox = kind;
    }
}