public sealed partial class YarnCommandBridge : InlineEventMarkupHandler.IInlineEmojiHost
{
    private const string EmojiSlot00 = "0";
    private const string EmojiSlot01 = "1";
    private const string EmojiSlot02 = "2";

    private enum EmojiEffect
    {
        Pop,
        Reveal,
        Conceal
    }

    public void PlayEmojiCue(string cue)
    {
        string characterKey = _vnRuntimeStateProvider != null
            ? _vnRuntimeStateProvider.CurrentCharacterKey
            : "";

        if (string.IsNullOrWhiteSpace(characterKey))
            return;

        if (string.IsNullOrWhiteSpace(cue))
        {
            HideInlineEmojiByCharacterNow(characterKey);
            return;
        }

        PlayInlineEmojiByCharacterNow(characterKey, cue);
    }

    private SetCharacterEmojiCommandSpecCharR BuildSetCharacterEmojiSpec(
        string roleKey,
        string emojiKey,
        string slotName = EmojiSlot00,
        float initialReveal = 1f,
        float fadeIn = 0.08f)
    {
        CharacterEmojiSlotParser.TryParse(
            slotName,
            out CharacterRigTarget rootTarget,
            out CharacterRigTarget castTarget,
            out CharacterRigTarget imageTarget);

        return new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,

            rootTarget = rootTarget,
            castTarget = castTarget,
            imageTarget = imageTarget,

            initialReveal = initialReveal,
            fadeIn = fadeIn
        };
    }

    private RevealCharacterEmojiCommandSpecCharR BuildRevealCharacterEmojiSpec(
        string roleKey,
        string slotName = EmojiSlot00,
        bool reverse = false,
        float duration = 1.2f)
    {
        CharacterEmojiSlotParser.TryParse(
            slotName,
            out _,
            out _,
            out CharacterRigTarget imageTarget);

        return new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            imageTarget = imageTarget,

            usePresetReveal = false,

            fromReveal = reverse ? 1f : 0f,
            toReveal = reverse ? 0f : 1f,
            duration = duration,

            wait = false,
            killTween = true
        };
    }

    private void EnqueueSetCharacterEmojiSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetCharacterEmojiSpec(
            roleKey,
            emojiKey,
            EmojiSlot00,
            initialReveal: 1f,
            fadeIn: 0.08f));
    }

    private void EnqueueSetCharacterEmojiSlotSpec(string roleKey, string emojiKey, string slotName)
    {
        Collect(BuildSetCharacterEmojiSpec(
            roleKey,
            emojiKey,
            slotName,
            initialReveal: 1f,
            fadeIn: 0.08f));
    }

    private void EnqueueEmojiFxSpec(string roleKey, string emojiKey, string effect)
    {
        EnqueueEmojiFx(roleKey, emojiKey, EmojiSlot00, effect);
    }

    private void EnqueueEmojiSlotFxSpec(string roleKey, string emojiKey, string slotName, string effect)
    {
        EnqueueEmojiFx(roleKey, emojiKey, slotName, effect);
    }

    private void EnqueueEmojiFx(string roleKey, string emojiKey, string slotName, string effect)
    {
        switch (ParseEmojiEffect(effect))
        {
            case EmojiEffect.Reveal:
                Collect(BuildSetCharacterEmojiSpec(
                    roleKey,
                    emojiKey,
                    slotName,
                    initialReveal: 0f,
                    fadeIn: 0f));

                Collect(BuildRevealCharacterEmojiSpec(
                    roleKey,
                    slotName,
                    reverse: false,
                    duration: 0.12f));
                return;

            case EmojiEffect.Conceal:
                Collect(BuildSetCharacterEmojiSpec(
                    roleKey,
                    emojiKey,
                    slotName,
                    initialReveal: 1f,
                    fadeIn: 0f));

                Collect(BuildRevealCharacterEmojiSpec(
                    roleKey,
                    slotName,
                    reverse: true,
                    duration: 0.12f));
                return;

            case EmojiEffect.Pop:
            default:
                Collect(BuildSetCharacterEmojiSpec(
                    roleKey,
                    emojiKey,
                    slotName,
                    initialReveal: 1f,
                    fadeIn: 0.08f));
                return;
        }
    }

    private void EnqueueHideCharacterEmojiSpec(string roleKey)
    {
        Collect(BuildSetCharacterEmojiSpec(roleKey, ""));
    }

    private void EnqueueHideCharacterEmojiSlotSpec(string roleKey, string slotName)
    {
        Collect(BuildSetCharacterEmojiSpec(roleKey, "", slotName));
    }

    private void EnqueueRevealCharacterEmojiSpec(string roleKey)
    {
        Collect(BuildRevealCharacterEmojiSpec(roleKey));
    }

    private void EnqueueRevealCharacterEmojiSlotSpec(string roleKey, string slotName)
    {
        Collect(BuildRevealCharacterEmojiSpec(roleKey, slotName));
    }

    private void EnqueueConcealCharacterEmojiSpec(string roleKey)
    {
        Collect(BuildRevealCharacterEmojiSpec(roleKey, EmojiSlot00, reverse: true));
    }

    private void EnqueueConcealCharacterEmojiSlotSpec(string roleKey, string slotName)
    {
        Collect(BuildRevealCharacterEmojiSpec(roleKey, slotName, reverse: true));
    }

    private static EmojiEffect ParseEmojiEffect(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect))
            return EmojiEffect.Pop;

        switch (effect.Trim().ToLowerInvariant())
        {
            case "reveal":
            case "wipe":
            case "show":
                return EmojiEffect.Reveal;

            case "conceal":
            case "hide":
            case "close":
                return EmojiEffect.Conceal;

            case "pop":
            case "default":
            case "normal":
                return EmojiEffect.Pop;

            default:
                UnityEngine.Debug.LogWarning(
                    $"[YarnCommandBridge] Unknown emoji effect '{effect}'. Fallback to Pop.");
                return EmojiEffect.Pop;
        }
    }
}