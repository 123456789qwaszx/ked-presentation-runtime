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
    void SetVisible(bool visible);
}