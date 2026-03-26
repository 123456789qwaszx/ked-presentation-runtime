using TMPro;
using UnityEngine;

public sealed class DialogueBox00_WithPortrait : UIBase<DialogueBox00_WithPortrait.Refs>, IManagedUI, IDialogueBoxView
{
    public enum Refs
    {
        DialogueBox00_Root,

        DialogueBox00BG_Root,
        DialogueBox00BG_Image,

        DialogueBox00TextArea_Root,
        DialogueBox00TextArea_Anchor,
        DialogueBox00TextArea_Text,

        DialogueBox00AdvanceIndicator_Root,
        DialogueBox00AdvanceIndicator_Anchor,
        DialogueBox00AdvanceIndicator_Image,

        DialogueBox00ProtagonistCutin_Slot,
        DialogueBox00ProtagonistCutinViewport_Mask,

        DialogueBox00TextAreaSpeakerName_Text,
    }

    public RectTransform ProtagonistRect => View?.Rect(Refs.DialogueBox00ProtagonistCutinViewport_Mask);
    // ---- IDialogueBoxView
    public TMP_Text LineText => View?.Text(Refs.DialogueBox00TextArea_Text);

    public TMP_Text NameText => View?.Text(Refs.DialogueBox00TextAreaSpeakerName_Text);
    public bool HasName => true;

    public void SetVisible(bool visible) => gameObject.SetActive(visible);

    protected override void Initialize()
    {
        gameObject.SetActive(true);
    }
}