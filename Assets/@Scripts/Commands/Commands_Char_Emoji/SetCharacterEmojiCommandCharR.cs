using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Set Character Emoji Image", Order = -700)]
public sealed class SetCharacterEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Emoji Identity")]
    public string emojiKey;

    [Header("Rig Targets")]
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Image")]
    public bool preserveAspect = true;
    public bool setNativeSize = false;
}

public sealed class SetCharacterEmojiCommandCharR : CommandBase
{
    private readonly SetCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetCharacterEmojiCommandCharR(
        SetCharacterEmojiCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        Image image = rigRefs.GetImage(_spec.imageTarget);
        
        if (!TryResolveSprite(out Sprite sprite))
            yield break;

        image.sprite = sprite;
        image.preserveAspect = _spec.preserveAspect;

        if (_spec.setNativeSize)
            image.SetNativeSize();
    }

    private bool TryResolveSprite(out Sprite sprite)
    {
        sprite = null;

        if (_resolver.TryResolveSprite(_spec.emojiKey, out sprite))
            return true;

        Debug.LogWarning(
            $"[SetCharacterEmojiCommandCharR] Failed to resolve emoji sprite. " +
            $"emojiKey='{_spec.emojiKey}', targetKey='{_spec.slotKey}'.");

        return false;
    }
}