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
        out CharacterEmojiLayout layout)
    {
        sprite = null;
        layout = CharacterEmojiLayout.Default;

        if (_library == null)
            return false;

        if (!_library.TryGet(emojiKey, out CharacterEmojiEntry entry))
            return false;

        if (entry.sprite == null)
            return false;

        sprite = entry.sprite;
        layout = entry.layout;
        return true;
    }
}