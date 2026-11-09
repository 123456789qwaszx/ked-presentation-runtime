using TMPro;

public interface IPresentationDialogueBoxView : IPresentationLineSurfaceView
{
    TMP_Text GetLineText();
    TMP_Text GetNameText();

    void ApplySurfaceLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry);

    void ResetPresentationTransform();
    void PrimeText(string text, string characterName, bool hasCharacterName);
}
