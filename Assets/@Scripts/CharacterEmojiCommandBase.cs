using UnityEngine;
using UnityEngine.UI;

public abstract class CharacterEmojiCommandBase : CommandBase
{
    protected static CharacterEmojiMirrorContext ResolveEmojiMirrorContext(
        CommandRunScope scope,
        CharacterEmojiResolver resolver,
        string slotKey,
        string emojiKey)
    {
        CharacterFacing facing = CharacterFacing.Right;
        scope?.CastRegistry?.TryPeekFacing(slotKey, out facing);

        CharacterEmojiMirrorProfile profile = CharacterEmojiMirrorProfile.Default;

        if (resolver != null &&
            resolver.TryResolveMirrorProfile(emojiKey, out CharacterEmojiMirrorProfile resolvedProfile) &&
            resolvedProfile != null)
        {
            profile = resolvedProfile;
        }

        return new CharacterEmojiMirrorContext(facing, profile);
    }

    protected static void ApplySpriteMirror(Image image, CharacterEmojiMirrorContext context)
    {
        if (image == null)
            return;

        RectTransform rect = image.rectTransform;
        if (rect == null)
            return;

        Vector3 scale = rect.localScale;
        float xAbs = Mathf.Abs(scale.x);

        if (xAbs <= 0.0001f)
            xAbs = 1f;

        rect.localScale = new Vector3(
            context.ShouldMirrorSprite ? -xAbs : xAbs,
            scale.y,
            scale.z);
    }

    protected static Vector2 GetSignedDirection(CharRigDirection direction)
    {
        switch (direction)
        {
            case CharRigDirection.Left:
                return Vector2.left;

            case CharRigDirection.Right:
                return Vector2.right;

            case CharRigDirection.Up:
                return Vector2.up;

            case CharRigDirection.Down:
                return Vector2.down;

            default:
                return Vector2.zero;
        }
    }
}