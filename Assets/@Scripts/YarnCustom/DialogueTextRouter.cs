using TMPro;
using UnityEngine;

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