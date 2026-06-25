using TMPro;
using UnityEngine;

public sealed class DialogueBox03_OnlyText
    : PresentationDialogueBoxViewBase<DialogueBox03_OnlyText.Refs>
{
    public enum Refs
    {
        DialogueBox03_Root,

        DialogueBox03BG_Root,
        DialogueBox03BG_Image,

        DialogueBox03TextArea_Root,
        DialogueBox03TextArea_Anchor,
        DialogueBox03TextArea_Text,

        DialogueBox03AdvanceIndicator_Root,
        DialogueBox03AdvanceIndicator_Anchor,
        DialogueBox03AdvanceIndicator_Image,

        DialogueBox03SpeakerNameBox_Root,
        DialogueBox03SpeakerNameBox_Anchor,
        DialogueBox03SpeakerNameBox_Pad,
        DialogueBox03SpeakerNameBox_Image,

        DialogueBox03SpeakerNameBoxTextArea_Anchor,
        DialogueBox03SpeakerNameBoxTextArea_Text
    }

    public override RectTransform Root => View?.Rect(Refs.DialogueBox03_Root);
    public override CanvasGroup CanvasGroup => View?.CanvasGroup(Refs.DialogueBox03_Root);

    public override TMP_Text LineText => View?.Text(Refs.DialogueBox03TextArea_Text);

    public override TMP_Text NameText => null;
    public override bool HasName => false;
}