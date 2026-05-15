using System;
using TMPro;
using UnityEngine;

public sealed class DialogueBox03_OnlyText
    : UIBase<DialogueBox03_OnlyText.Refs>, IManagedUI, IPresentationDialogueBoxView
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

    public RectTransform Root => View.Rect(Refs.DialogueBox03_Root);
    public CanvasGroup CanvasGroup => View.CanvasGroup(Refs.DialogueBox03_Root);

    public TMP_Text LineText => View.Text(Refs.DialogueBox03TextArea_Text);
    public TMP_Text NameText => null;//View.Text(Refs.DialogueBox03SpeakerNameBoxTextArea_Text);
    public bool HasName => NameText != null;

    protected override void OnInitialize()
    {
        //gameObject.SetActive(true);
    }

    public void Validate()
    {
        if (Root == null)
            throw new InvalidOperationException($"[DialogueBox03_OnlyText] Missing Root. go={name}");

        if (CanvasGroup == null)
            throw new InvalidOperationException($"[DialogueBox03_OnlyText] Missing CanvasGroup. go={name}");

        if (LineText == null)
            throw new InvalidOperationException($"[DialogueBox03_OnlyText] Missing LineText. go={name}");
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
                throw new InvalidOperationException($"[DialogueBox03_OnlyText] Missing NameText. go={name}");

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