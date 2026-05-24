using TMPro;

public sealed class DialogueBoxTextPrimer
{
    public void Prime(IDialogueTextTarget target, VNDialogueLine line)
    {
        if (target == null || line == null)
            return;

        TMP_Text lineText = target.LineText;
        if (lineText != null)
        {
            lineText.text = line.Text;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
        }

        TMP_Text nameText = target.NameText;
        if (nameText != null)
        {
            bool showName = line.HasCharacterName;

            nameText.text = showName
                ? line.CharacterName
                : string.Empty;

            nameText.gameObject.SetActive(showName);
        }
    }
}