using TMPro;
using UnityEngine;

public interface IDialogueTextTarget
{
    TMP_Text LineText { get; }
    TMP_Text NameText { get; }
    bool HasName { get; }
    CanvasGroup CanvasGroup { get; }
}

public sealed class DialogueTextRouter : MonoBehaviour
{
    public TMP_Text LineText { get; private set; }
    public TMP_Text NameText { get; private set; }
    public bool HasName => NameText != null;

    public void Bind(IDialogueTextTarget box)
    {
        LineText = box.LineText;
        NameText = box.NameText;
    }

    public void Clear()
    {
        LineText = null;
        NameText = null;
    }
}

public sealed class DialogueBoxCurrentState
{
    public DialogueBoxKind? BoxKind { get; private set; }
    public IDialogueTextTarget Box { get; private set; }
    public bool IsVisible { get; private set; }

    public void Commit(
        DialogueBoxKind kind,
        IDialogueTextTarget box,
        DialogueBoxTransitionKind transitionKind)
    {
        BoxKind = kind;
        Box = box;
        IsVisible = transitionKind != DialogueBoxTransitionKind.Hide;
    }

    public void Reset()
    {
        BoxKind = null;
        Box = null;
        IsVisible = false;
    }
}