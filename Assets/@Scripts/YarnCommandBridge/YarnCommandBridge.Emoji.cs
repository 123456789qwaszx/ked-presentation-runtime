using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    [Header("Emoji")]
    [SerializeField] private CharacterEmojiDatabaseSO emojiDatabase;

    private void RegisterEmojiCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string>(
            "emoji",
            EnqueueShowEmojiSpec);

        _dialogueRunner.AddCommandHandler<string>(
            "emoji_hide",
            EnqueueHideEmojiSpec);
    }

    private ShowEmojiCommandSpecCharR BuildShowEmojiSpec(string roleKey, string emojiKey)
    {
        return new ShowEmojiCommandSpecCharR
        {
            roleKey = roleKey,
            database = emojiDatabase,
            emojiKey = emojiKey,
            hideIfKeyEmpty = true,
            wait = false,
            fadeInOverride = -1f,
            ease = Ease.OutCubic,
            snapOnSkip = true
        };
    }

    private void EnqueueShowEmojiSpec(string roleKey, string emojiKey)
    {
        Collect(BuildShowEmojiSpec(roleKey, emojiKey));
    }

    private void EnqueueHideEmojiSpec(string roleKey)
    {
        Collect(BuildShowEmojiSpec(roleKey, ""));
    }

    // Inline markup용: 현재 재생 중인 라인 도중 즉시 표시
    public void PlayInlineEmojiByCharacterNow(string roleKey, string cue)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] roleKey is null or empty.", this);
            return;
        }

        string emojiKey = cue ?? "";

        var specs = new List<CommandSpecBase>(1)
        {
            BuildShowEmojiSpec(roleKey, emojiKey)
        };

        _playbackDriver.PlayImmediate(specs, "inline-emoji");
    }

    public void HideInlineEmojiByCharacterNow(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] roleKey is null or empty.", this);
            return;
        }

        var specs = new List<CommandSpecBase>(1)
        {
            BuildShowEmojiSpec(roleKey, "")
        };

        _playbackDriver.PlayImmediate(specs, "inline-emoji-hide");
    }
}