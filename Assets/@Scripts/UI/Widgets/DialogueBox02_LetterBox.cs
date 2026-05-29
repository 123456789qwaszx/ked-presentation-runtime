using System;
using TMPro;
using UnityEngine;

public sealed class DialogueBox02_LetterBox
    : UIBase<DialogueBox02_LetterBox.Refs>, IManagedUI, IPresentationDialogueBoxView
{
    public enum Refs
    {
        DialogueBox02_Root,

        DialogueBox02BottomTextArea_Text,
        DialogueBox02BottomTextAreaSpeakerName_Text,
    }

    public RectTransform Root => View?.Rect(Refs.DialogueBox02_Root);
    public CanvasGroup CanvasGroup => View?.CanvasGroup(Refs.DialogueBox02_Root);

    public TMP_Text LineText => View.Text(Refs.DialogueBox02BottomTextArea_Text);

    // LetterBox는 이름을 쓰지 않는 박스로 고정한다면 null 유지.
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