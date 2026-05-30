public sealed partial class YarnCommandBridge : InlineEventMarkupHandler.IInlineEmojiHost
{
    private const string EmojiSlot00 = "0";
    private const string EmojiSlot01 = "1";
    private const string EmojiSlot02 = "2";

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
        string slotName = EmojiSlot00)
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
        };
    }

    private void EnqueueSetCharacterEmojiSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetCharacterEmojiSpec(roleKey, emojiKey));
    }

    private void EnqueueSetCharacterEmojiSlotSpec(string roleKey, string emojiKey, string slotName)
    { 
        Collect(BuildSetCharacterEmojiSpec(roleKey, emojiKey, slotName));
    }

    private void EnqueueHideCharacterEmojiSpec(string roleKey)
    {
        Collect(BuildSetCharacterEmojiSpec(roleKey, ""));
    }

    private void EnqueueHideCharacterEmojiSlotSpec(string roleKey, string slotName)
    {
        Collect(BuildSetCharacterEmojiSpec(roleKey, "", slotName));
    }
}