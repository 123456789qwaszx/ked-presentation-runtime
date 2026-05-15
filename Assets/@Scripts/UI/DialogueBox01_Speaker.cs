using System;
using TMPro;
using UnityEngine;

public sealed class DialogueBox01_Speaker
    : UIBase<DialogueBox01_Speaker.Refs>, IManagedUI, IPresentationDialogueBoxView
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

    public RectTransform Root => View.Rect(Refs.DialogueBox01_Root);
    public CanvasGroup CanvasGroup => View.CanvasGroup(Refs.DialogueBox01_Root);

    public TMP_Text LineText => View.Text(Refs.DialogueBox01TextArea_Text);
    public TMP_Text NameText => View.Text(Refs.DialogueBox01SpeakerNameBoxTextArea_Text);
    public bool HasName => NameText != null;

    protected override void OnInitialize()
    {
        //gameObject.SetActive(true);
    }

    public void Validate()
    {
        if (Root == null)
            throw new InvalidOperationException($"[DialogueBox01_NoPortrait] Missing Root. go={name}");

        if (CanvasGroup == null)
            throw new InvalidOperationException($"[DialogueBox01_NoPortrait] Missing CanvasGroup. go={name}");

        if (LineText == null)
            throw new InvalidOperationException($"[DialogueBox01_NoPortrait] Missing LineText. go={name}");
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
        if (NameText == null)
        {
            if (strict)
                throw new InvalidOperationException($"[DialogueBox01_NoPortrait] Missing NameText. go={name}");

            return;
        }

        NameText.text = text ?? string.Empty;
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

        if (NameText != null)
            NameText.text = string.Empty;
    }
}