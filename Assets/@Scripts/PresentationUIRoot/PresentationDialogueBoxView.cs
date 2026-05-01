using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IPresentationDialogueBoxView : IDialogueTextTarget
{
    RectTransform Root { get; }

    void Validate();
    void SetVisible(bool visible);
    void ClearText();
    void SetLineText(string text);
    void SetNameText(string text, bool strict = true);
    void SetDialogueText(string line, bool setName, string speakerName, bool strict = true);
}

public sealed class PresentationDialogueBoxView : MonoBehaviour, IDialogueTextTarget
{
    [Header("Explicit Bindings")]
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private TMP_Text nameText;

    public RectTransform Root => root;
    public CanvasGroup CanvasGroup => canvasGroup;

    public TMP_Text LineText => lineText;
    public TMP_Text NameText => nameText;
    public bool HasName => nameText != null;

    public void Validate()
    {
        if (root == null)
            throw new InvalidOperationException($"[PresentationDialogueBoxView] Missing Root. go={name}");

        if (canvasGroup == null)
            throw new InvalidOperationException($"[PresentationDialogueBoxView] Missing CanvasGroup. go={name}");

        if (lineText == null)
            throw new InvalidOperationException($"[PresentationDialogueBoxView] Missing LineText. go={name}");
    }

    public void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    public void SetLineText(string text)
    {
        lineText.text = text ?? string.Empty;
    }

    public void SetNameText(string text, bool strict = true)
    {
        if (nameText == null)
        {
            if (strict)
                throw new InvalidOperationException($"[PresentationDialogueBoxView] Missing NameText. go={name}");

            return;
        }

        nameText.text = text ?? string.Empty;
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
        lineText.text = string.Empty;

        if (nameText != null)
            nameText.text = string.Empty;
    }
}

public static class PresentationDialogueBoxRegistryExt
{
    public static string MakeDialogueBoxRefKey(string dialogueKey)
    {
        dialogueKey = SafeTrim(dialogueKey);
        return $"dlg:{dialogueKey}";
    }

    public static bool TryGetDialogueBoxView(this Dictionary<string, object> refs, string dialogueKey, out PresentationDialogueBoxView view)
    {
        string key = MakeDialogueBoxRefKey(dialogueKey);

        if (refs != null && refs.TryGetValue(key, out object obj) && obj is PresentationDialogueBoxView typed)
        {
            view = typed;
            return true;
        }

        view = null;
        return false;
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}