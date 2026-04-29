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

    public RectTransform Root => View.Rect(Refs.DialogueBox02_Root);
    public CanvasGroup CanvasGroup => View.CanvasGroup(Refs.DialogueBox02_Root);

    public TMP_Text LineText => View.Text(Refs.DialogueBox02BottomTextArea_Text);

    // LetterBox는 이름을 쓰지 않는 박스로 고정한다면 null 유지.
    public TMP_Text NameText => null;
    public bool HasName => false;

    protected override void Initialize()
    {
        //gameObject.SetActive(true);
    }

    public void Validate()
    {
        if (Root == null)
            throw new InvalidOperationException($"[DialogueBox02_LetterBox] Missing Root. go={name}");

        if (CanvasGroup == null)
            throw new InvalidOperationException($"[DialogueBox02_LetterBox] Missing CanvasGroup. go={name}");

        if (LineText == null)
            throw new InvalidOperationException($"[DialogueBox02_LetterBox] Missing LineText. go={name}");
    }

    public void SetVisible(bool visible)
    {
        CanvasGroup.alpha = visible ? 1f : 0f;
        CanvasGroup.interactable = visible;
        CanvasGroup.blocksRaycasts = visible;
    }

    public void SetLineText(string text)
    {
        LineText.text = text ?? string.Empty;
    }

    public void SetNameText(string text, bool strict = true)
    {
        if (strict)
            throw new InvalidOperationException($"[DialogueBox02_LetterBox] LetterBox does not support NameText. go={name}");
    }

    public void SetDialogueText(
        string line,
        bool setName,
        string speakerName,
        bool strict = true)
    {
        SetLineText(line);

        if (setName)
            SetNameText(speakerName, strict);
    }

    public void ClearText()
    {
        LineText.text = string.Empty;
    }
}