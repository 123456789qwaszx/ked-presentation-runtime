using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string EmojiSlot00 = "0";
    private const string EmojiSlot01 = "1";
    private const string EmojiSlot02 = "2";

    private void RegisterEmojiCommands()
    {
        // 기본 슬롯: EmojiSlot00
        // <<emoji nayu surprise>>
        _dialogueRunner.AddCommandHandler<string, string>(
            "emoji",
            EnqueueSetCharacterEmojiSpec);

        // 슬롯 지정
        // <<emoji_slot nayu surprise 1>>
        _dialogueRunner.AddCommandHandler<string, string, string>("emoji_slot", EnqueueSetCharacterEmojiSlotSpec);

        // 기본 슬롯 숨김: EmojiSlot00
        // <<emoji_hide nayu>>
        _dialogueRunner.AddCommandHandler<string>(
            "emoji_hide",
            EnqueueHideCharacterEmojiSpec);

        // 슬롯 지정 숨김
        // <<emoji_hide_slot nayu 1>>
        _dialogueRunner.AddCommandHandler<string, string>(
            "emoji_hide_slot",
            EnqueueHideCharacterEmojiSlotSpec);
    }

    private SetCharacterEmojiCommandSpecCharR BuildSetCharacterEmojiSpec(
        string roleKey,
        string emojiKey,
        string slotName = EmojiSlot00)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] roleKey is null or empty.", this);
            return null;
        }

        ResolveEmojiTargets(
            slotName,
            out CharacterRigTarget rootTarget,
            out CharacterRigTarget castTarget,
            out CharacterRigTarget imageTarget);

        return new SetCharacterEmojiCommandSpecCharR
        {
            targetKey = roleKey.Trim(),

            emojiKey = emojiKey ?? "",

            rootTarget = rootTarget,
            castTarget = castTarget,
            imageTarget = imageTarget,

            useResolvedLayout = true,
            overrideLayout = false,

            alpha = 1f,
            fadeIn = 0.08f,
            fadeEase = Ease.OutCubic,

            resetCastTransform = true,
            killTween = true,

            wait = false
        };
    }

    private void EnqueueSetCharacterEmojiSpec(string roleKey, string emojiKey)
    {
        CollectIfValid(BuildSetCharacterEmojiSpec(roleKey, emojiKey, EmojiSlot00));
    }

    private void EnqueueSetCharacterEmojiSlotSpec(
        string roleKey,
        string emojiKey,
        string slotName)
    {
        CollectIfValid(BuildSetCharacterEmojiSpec(roleKey, emojiKey, slotName));
    }

    private void EnqueueHideCharacterEmojiSpec(string roleKey)
    {
        CollectIfValid(BuildSetCharacterEmojiSpec(roleKey, "", EmojiSlot00));
    }

    private void EnqueueHideCharacterEmojiSlotSpec(string roleKey, string slotName)
    {
        CollectIfValid(BuildSetCharacterEmojiSpec(roleKey, "", slotName));
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

        SetCharacterEmojiCommandSpecCharR spec =
            BuildSetCharacterEmojiSpec(roleKey, emojiKey, EmojiSlot00);

        if (spec == null)
            return;

        var specs = new List<CommandSpecBase>(1)
        {
            spec
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

        SetCharacterEmojiCommandSpecCharR spec =
            BuildSetCharacterEmojiSpec(roleKey, "", EmojiSlot00);

        if (spec == null)
            return;

        var specs = new List<CommandSpecBase>(1)
        {
            spec
        };

        _playbackDriver.PlayImmediate(specs, "inline-emoji-hide");
    }

    private void CollectIfValid(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        Collect(spec);
    }

    private void ResolveEmojiTargets(
        string slotName,
        out CharacterRigTarget rootTarget,
        out CharacterRigTarget castTarget,
        out CharacterRigTarget imageTarget)
    {
        string normalized = string.IsNullOrWhiteSpace(slotName)
            ? EmojiSlot00
            : slotName.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case EmojiSlot01:
            case "slot1":
            case "slot01":
            case "emoji1":
            case "emoji01":
                rootTarget = CharacterRigTarget.CharacterEmojiSlot01_Root;
                castTarget = CharacterRigTarget.CharacterEmojiSlot01_CastTransform;
                imageTarget = CharacterRigTarget.EmojiSlot01_Image;
                return;

            case EmojiSlot02:
            case "slot2":
            case "slot02":
            case "emoji2":
            case "emoji02":
                rootTarget = CharacterRigTarget.CharacterEmojiSlot02_Root;
                castTarget = CharacterRigTarget.CharacterEmojiSlot02_CastTransform;
                imageTarget = CharacterRigTarget.EmojiSlot02_Image;
                return;

            case EmojiSlot00:
            case "slot0":
            case "slot00":
            case "emoji0":
            case "emoji00":
            default:
                rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
                castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform;
                imageTarget = CharacterRigTarget.EmojiSlot00_Image;
                return;
        }
    }
}