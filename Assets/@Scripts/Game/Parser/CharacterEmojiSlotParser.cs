public static class CharacterEmojiSlotParser
{
    public const string EmojiSlot00 = "emoji_slot_00";
    public const string EmojiSlot01 = "emoji_slot_01";
    public const string EmojiSlot02 = "emoji_slot_02";

    public static void ParseOrDefault(
        string slotName,
        out CharacterRigTarget rootTarget,
        out CharacterRigTarget castTarget,
        out CharacterRigTarget imageTarget)
    {
        TryParse(
            slotName,
            out rootTarget,
            out castTarget,
            out imageTarget);
    }

    public static bool TryParse(
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
                return true;

            case EmojiSlot02:
            case "slot2":
            case "slot02":
            case "emoji2":
            case "emoji02":
                rootTarget = CharacterRigTarget.CharacterEmojiSlot02_Root;
                castTarget = CharacterRigTarget.CharacterEmojiSlot02_CastTransform;
                imageTarget = CharacterRigTarget.EmojiSlot02_Image;
                return true;

            case EmojiSlot00:
            case "slot0":
            case "slot00":
            case "emoji0":
            case "emoji00":
                rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
                castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform;
                imageTarget = CharacterRigTarget.EmojiSlot00_Image;
                return true;

            default:
                rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
                castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform;
                imageTarget = CharacterRigTarget.EmojiSlot00_Image;
                return false;
        }
    }
}