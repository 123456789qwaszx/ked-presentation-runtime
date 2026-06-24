using TMPro;
using UnityEngine;

public sealed class DialogueSurfaceBox
    : PresentationDialogueBoxViewBase<DialogueSurfaceBox.Refs>
{
    public enum Refs
    {
        SurfaceBox_Root,
        SurfaceBoxLine_Text,
        SurfaceBoxName_Text,
    }

    public override RectTransform Root
        => View.Rect(Refs.SurfaceBox_Root);

    public override CanvasGroup CanvasGroup
        => View.CanvasGroup(Refs.SurfaceBox_Root);

    public override TMP_Text LineText
        => View.Text(Refs.SurfaceBoxLine_Text);

    public override TMP_Text NameText
        => View.Text(Refs.SurfaceBoxName_Text);
}