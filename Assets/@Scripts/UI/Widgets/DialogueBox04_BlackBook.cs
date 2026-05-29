using System;
using TMPro;
using UnityEngine;

public sealed class DialogueBox04_BlackBook
    : UIBase<DialogueBox04_BlackBook.Refs>, IManagedUI, IPresentationDialogueBoxView
{
    public enum Refs
    {
        DialogueBox04_Root,
        DialogueBox04Area_Text
    }

    public RectTransform Root => View?.Rect(Refs.DialogueBox04_Root);
    public CanvasGroup CanvasGroup => View?.CanvasGroup(Refs.DialogueBox04_Root);

    public TMP_Text LineText => View.Text(Refs.DialogueBox04Area_Text);

    public TMP_Text NameText => null;
    public bool HasName => false;

    public void SetVisible(bool visible)
    {
        if (CanvasGroup == null)
            return;
        
        CanvasGroup.alpha = visible ? 1f : 0f;
        CanvasGroup.interactable = visible;
        CanvasGroup.blocksRaycasts = visible;
    }
}