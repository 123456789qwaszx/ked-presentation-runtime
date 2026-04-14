using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EmojiCueMapEntry
{
    public string cue;
    public string spriteKey;
}

public interface IEmojiSpriteResolver
{
    bool TryResolve(string spriteKey, out Sprite sprite);
}

public sealed class ResourcesEmojiSpriteResolver : IEmojiSpriteResolver
{
    public bool TryResolve(string spriteKey, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrWhiteSpace(spriteKey))
            return false;

        sprite = Resources.Load<Sprite>(spriteKey);
        return sprite != null;
    }
}


public sealed class InlineEmojiHost : MonoBehaviour, InlineEventMarkupHandler.IInlineEmojiHost
{
    private YarnCommandBridge _commandBridge;

    public void Initialize(YarnCommandBridge commandBridge)
    {
        _commandBridge = commandBridge;
    }

    public void PlayEmojiCue(string characterKey, string cue)
    {
        if (_commandBridge == null)
        {
            Debug.LogWarning("[InlineEmojiHost] YarnCommandBridge is null.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(characterKey))
        {
            Debug.LogWarning("[InlineEmojiHost] characterKey is null or empty.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(cue))
        {
            _commandBridge.EnqueueInlineEmojiHideByCharacter(characterKey);
            return;
        }
        _commandBridge.PlayInlineEmojiByCharacterNow(characterKey, cue);
    }
}