using TMPro;

public sealed class DialogueBox03_OnlyText
    : UIBase<DialogueBox03_OnlyText.Refs>, IManagedUI, IDialogueBoxView
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

    // ---- IDialogueBoxView
    public TMP_Text LineText => View.Text(Refs.DialogueBox03TextArea_Text);
    public TMP_Text NameText => View.Text(Refs.DialogueBox03SpeakerNameBoxTextArea_Text);
    public bool HasName => NameText != null;

    public void SetVisible(bool visible) => gameObject.SetActive(visible);
}