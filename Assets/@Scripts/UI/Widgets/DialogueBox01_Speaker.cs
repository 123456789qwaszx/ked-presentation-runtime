using TMPro;
using UnityEngine;

public sealed class DialogueBox01_Speaker
    : PresentationDialogueBoxViewBase<DialogueBox01_Speaker.Refs>
{
    public enum Refs
    {
        DialogueBox01_Root,

        DialogueBox01BG_Root,
        DialogueBox01BG_Image,

        DialogueBox01TextArea_Root,
        DialogueBox01TextArea_Anchor,
        DialogueBox01TextArea_Text,

        DialogueBox01AdvanceIndicator_Root,
        DialogueBox01AdvanceIndicator_Anchor,
        DialogueBox01AdvanceIndicator_Image,

        DialogueBox01SpeakerNameBox_Root,
        DialogueBox01SpeakerNameBox_Anchor,
        DialogueBox01SpeakerNameBox_Pad,
        DialogueBox01SpeakerNameBox_Image,

        DialogueBox01SpeakerNameBoxTextArea_Anchor,
        DialogueBox01SpeakerNameBoxTextArea_Text
    }

    public override RectTransform Root => View?.Rect(Refs.DialogueBox01_Root);
    public override CanvasGroup CanvasGroup => View?.CanvasGroup(Refs.DialogueBox01_Root);

    public override TMP_Text LineText => View.Text(Refs.DialogueBox01TextArea_Text);
    public override TMP_Text NameText => View.Text(Refs.DialogueBox01SpeakerNameBoxTextArea_Text);
}