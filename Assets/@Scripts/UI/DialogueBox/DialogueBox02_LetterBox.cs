using TMPro;
using UnityEngine;

public sealed class DialogueBox02_LetterBox
    : PresentationDialogueBoxViewBase<DialogueBox02_LetterBox.Refs>
{
    public enum Refs
    {
        DialogueBox02_Root,

        DialogueBox02BottomTextArea_Text,
        DialogueBox02BottomTextAreaSpeakerName_Text,
    }

    public override RectTransform Root => View?.Rect(Refs.DialogueBox02_Root);
    public override CanvasGroup CanvasGroup => View?.CanvasGroup(Refs.DialogueBox02_Root);

    public override TMP_Text LineText => View?.Text(Refs.DialogueBox02BottomTextArea_Text);

    public override TMP_Text NameText => null;
    public override bool HasName => false;
}