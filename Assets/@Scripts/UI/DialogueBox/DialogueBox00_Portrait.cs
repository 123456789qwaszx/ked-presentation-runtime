using TMPro;
using UnityEngine;

public sealed partial class DialogueBox00_Portrait
    : PresentationDialogueBoxViewBase<DialogueBox00_Portrait.Refs>
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

    public override RectTransform Root => View?.Rect(Refs.DialogueBox00_Root);
    public override CanvasGroup CanvasGroup => View?.CanvasGroup(Refs.DialogueBox00_Root);

    public override TMP_Text LineText => View?.Text(Refs.DialogueBox00TextArea_Text);

    public override TMP_Text NameText => null;

    public override bool HasName => false;
}