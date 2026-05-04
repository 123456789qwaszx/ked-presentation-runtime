using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EmojiCueMapEntry
{
    public string cue;
    public string spriteKey;
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
        if (string.IsNullOrWhiteSpace(cue))
        {
            _commandBridge.EnqueueInlineEmojiHideByCharacter(characterKey);
            return;
        }
        _commandBridge.PlayInlineEmojiByCharacterNow(characterKey, cue);
    }
}