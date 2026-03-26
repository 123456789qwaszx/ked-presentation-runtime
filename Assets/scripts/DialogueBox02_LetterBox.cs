using TMPro;

public sealed class DialogueBox02_LetterBox : UIBase<DialogueBox02_LetterBox.Refs>, IManagedUI, IDialogueBoxView
{
    public enum Refs
    {
        DialogueBox02BottomTextArea_Text,
        DialogueBox02BottomTextAreaSpeakerName_Text,
    }

    public TMP_Text LineText => View?.Text(Refs.DialogueBox02BottomTextArea_Text);
    public TMP_Text NameText => null;
    public bool HasName => false;

    public void SetVisible(bool visible) => gameObject.SetActive(visible);

    protected override void Initialize()
    {
        gameObject.SetActive(true);
    }
}