using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

public sealed class EmojiEventMarkupHandler : ActionMarkupHandler
{
    private CharacterEmojiAnchor _target;
    private readonly Dictionary<int, string> _emojiEvents = new();

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        _target = null;
        _emojiEvents.Clear();

        if (!line.TryGetAttributeWithName("character", out var character))
        {
            Debug.LogWarning("[EmojiEvent] line has no character attribute.");
            return;
        }

        if (!character.Properties.TryGetValue("name", out var name))
        {
            Debug.LogWarning("[EmojiEvent] character attribute has no name property.");
            return;
        }

        string characterName = name.StringValue;

        GameObject targetObject = GameObject.Find(characterName);
        if (targetObject == null)
        {
            Debug.LogWarning($"[EmojiEvent] scene has no GameObject named '{characterName}'.");
            return;
        }

        _target = targetObject.GetComponent<CharacterEmojiAnchor>();
        if (_target == null)
        {
            Debug.LogWarning($"[EmojiEvent] '{characterName}' has no CharacterEmojiAnchor.");
            return;
        }

        foreach (MarkupAttribute attribute in line.Attributes)
        {
            if (attribute.Name != "emoji")
                continue;

            if (!attribute.TryGetProperty("key", out string emojiKey))
                continue;

            _emojiEvents[attribute.Position] = emojiKey;
        }
    }

    public override async YarnTask OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        if (_target == null)
            return;

        if (!_emojiEvents.TryGetValue(currentCharacterIndex, out string emojiKey))
            return;

        _target.ShowEmoji(emojiKey);

        await YarnTask.CompletedTask;
    }

    public override void OnLineWillDismiss()
    {
        if (_target == null)
            return;

        _target.HideEmoji();
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) { }
    public override void OnLineDisplayComplete() { }
}