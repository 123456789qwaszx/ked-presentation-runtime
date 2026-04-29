using TMPro;
using UnityEngine;

public interface IDialogueTextTarget
{
    TMP_Text LineText { get; }
    TMP_Text NameText { get; }
    bool HasName { get; }
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