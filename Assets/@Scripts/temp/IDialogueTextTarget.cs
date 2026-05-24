using TMPro;
using UnityEngine;

public interface IDialogueTextTarget
{
    TMP_Text LineText { get; }
    TMP_Text NameText { get; }
    bool HasName { get; }
    CanvasGroup CanvasGroup { get; }
}

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