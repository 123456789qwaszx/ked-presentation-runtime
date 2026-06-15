using UnityEngine;

public sealed class CharacterEmojiResolver
{
    private readonly CharacterEmojiLibrarySO _library;

    public CharacterEmojiResolver(CharacterEmojiLibrarySO library)
    {
        _library = library;
    }

    public bool TryResolve(
        string emojiKey,
        out Sprite sprite,
        out CharacterEmojiPlacement placement,
        out CharacterEmojiVisualPresetSO visualPreset)
    {
        sprite = null;
        placement = CharacterEmojiPlacement.Default;
        visualPreset = null;

        if (_library == null)
            return false;

        string normalizedKey = NormalizeEmojiKey(emojiKey);

        if (!_library.TryGet(normalizedKey, out CharacterEmojiEntry entry))
            return false;

        if (entry.sprite == null)
            return false;

        sprite = entry.sprite;
        placement = entry.placement;
        visualPreset = entry.defaultVisualPreset;
        return true;
    }

    private static string NormalizeEmojiKey(string emojiKey)
    {
        if (string.IsNullOrWhiteSpace(emojiKey))
            return emojiKey;

        string trimmed = emojiKey.Trim();

        // Authoring shortcut:
        // "1", "9" resolve to "01", "09".
        // Existing keys like "07", "10" are preserved.
        if (trimmed.Length == 1 && trimmed[0] >= '1' && trimmed[0] <= '9')
            return "0" + trimmed;

        return trimmed;
    }
}