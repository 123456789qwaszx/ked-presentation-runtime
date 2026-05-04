using System;
using TMPro;
using UnityEngine;

public sealed class DialogueBox00_Portrait
    : UIBase<DialogueBox00_Portrait.Refs>, IManagedUI, IPresentationDialogueBoxView
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

    public RectTransform Root => View.Rect(Refs.DialogueBox00_Root);
    public CanvasGroup CanvasGroup => Root.GetComponent<CanvasGroup>();

    public TMP_Text LineText => View.Text(Refs.DialogueBox00TextArea_Text);
    public TMP_Text NameText => null;//=> View.Text(Refs.DialogueBox00TextAreaSpeakerName_Text);
    public bool HasName => true;

    public RectTransform ProtagonistRect => View.Rect(Refs.DialogueBox00ProtagonistCutinViewport_Mask);

    protected override void Initialize()
    {
        //gameObject.SetActive(true);
    }

    public void Validate()
    {
        if (Root == null)
            throw new InvalidOperationException($"[DialogueBox00_WithPortrait] Missing Root. go={name}");

        if (CanvasGroup == null)
            throw new InvalidOperationException($"[DialogueBox00_WithPortrait] Missing CanvasGroup. go={name}");

        if (LineText == null)
            throw new InvalidOperationException($"[DialogueBox00_WithPortrait] Missing LineText. go={name}");
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
                throw new InvalidOperationException($"[DialogueBox00_WithPortrait] Missing NameText. go={name}");

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