using TMPro;
using UnityEngine;

public sealed class DialogueBox04_BlackBook
    : PresentationDialogueBoxViewBase<DialogueBox04_BlackBook.Refs>
{
    public enum Refs
    {
        DialogueBox04_Root,
        DialogueBox04Area_Text
    }

    public override RectTransform Root => View?.Rect(Refs.DialogueBox04_Root);
    public override CanvasGroup CanvasGroup => View?.CanvasGroup(Refs.DialogueBox04_Root);

    public override TMP_Text LineText => View.Text(Refs.DialogueBox04Area_Text);

    public override TMP_Text NameText => null;
    public override bool HasName => false;
}