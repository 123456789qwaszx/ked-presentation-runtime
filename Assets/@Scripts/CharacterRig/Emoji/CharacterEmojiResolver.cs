using UnityEngine;

public sealed class CharacterEmojiResolver
{
    private readonly CharacterEmojiLibrarySO _library;

    public CharacterEmojiResolver(CharacterEmojiLibrarySO library)
    {
        _library = library;
    }

    public bool TryResolveEntry(
        string emojiKey,
        out CharacterEmojiEntry entry)
    {
        entry = null;

        if (_library == null)
            return false;

        string normalizedKey = NormalizeEmojiKey(emojiKey);

        return _library.TryGet(normalizedKey, out entry) &&
               entry != null;
    }

    public bool TryResolveSprite(
        string emojiKey,
        out Sprite sprite)
    {
        sprite = null;

        if (!TryResolveEntry(emojiKey, out CharacterEmojiEntry entry))
            return false;

        if (entry.sprite == null)
            return false;

        sprite = entry.sprite;
        return true;
    }

    public bool TryResolvePlacement(
        string emojiKey,
        out CharacterEmojiPlacement placement)
    {
        placement = CharacterEmojiPlacement.Default;

        if (!TryResolveEntry(emojiKey, out CharacterEmojiEntry entry))
            return false;

        placement = entry.placement;
        return true;
    }

    public bool TryResolveMirrorProfile(
        string emojiKey,
        out CharacterEmojiMirrorProfile mirrorProfile)
    {
        mirrorProfile = CharacterEmojiMirrorProfile.Default;

        if (!TryResolveEntry(emojiKey, out CharacterEmojiEntry entry))
            return false;

        mirrorProfile = entry.mirror ?? CharacterEmojiMirrorProfile.Default;
        return true;
    }

    public bool TryResolve(
        string emojiKey,
        out Sprite sprite,
        out CharacterEmojiPlacement placement)
    {
        sprite = null;
        placement = CharacterEmojiPlacement.Default;

        if (!TryResolveEntry(emojiKey, out CharacterEmojiEntry entry))
            return false;

        sprite = entry.sprite;
        placement = entry.placement;
        return true;
    }

    public bool TryResolve(
        string emojiKey,
        out Sprite sprite,
        out CharacterEmojiPlacement placement,
        out CharacterEmojiMirrorProfile mirrorProfile)
    {
        sprite = null;
        placement = CharacterEmojiPlacement.Default;
        mirrorProfile = CharacterEmojiMirrorProfile.Default;

        if (!TryResolveEntry(emojiKey, out CharacterEmojiEntry entry))
            return false;

        sprite = entry.sprite;
        placement = entry.placement;
        mirrorProfile = entry.mirror ?? CharacterEmojiMirrorProfile.Default;
        return true;
    }

    private static string NormalizeEmojiKey(string emojiKey)
    {
        if (string.IsNullOrWhiteSpace(emojiKey))
            return emojiKey;

        string trimmed = emojiKey.Trim();

        if (trimmed.Length == 1 && trimmed[0] >= '1' && trimmed[0] <= '9')
            return "0" + trimmed;

        return trimmed;
    }
}