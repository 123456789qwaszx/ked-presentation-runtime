using System;

public sealed class VNDialogueLine
{
    public string TextId { get; private set; }
    public string Text { get; private set; }
    public string CharacterName { get; private set; }
    public bool HasCharacterName { get; private set; }
    public string[] Metadata { get; private set; }

    public VNDialogueLine(
        string textId,
        string text,
        string characterName,
        string[] metadata)
    {
        TextId = textId ?? string.Empty;
        Text = text ?? string.Empty;
        CharacterName = characterName ?? string.Empty;
        Metadata = metadata ?? Array.Empty<string>();

        HasCharacterName = !string.IsNullOrWhiteSpace(CharacterName);
    }
}