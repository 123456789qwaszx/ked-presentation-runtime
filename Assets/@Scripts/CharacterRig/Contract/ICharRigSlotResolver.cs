using UnityEngine;

public enum CharRigSlot
{
    CharacterSlotRight = 0,
    CharacterSlotCenter = 1,
    CharacterSlotLeft = 2,
    ProtagonistSlot = 3,
    LiveChatIdolSlot00 = 100,
    LiveChatIdolSlot01 = 101
}

public interface ICharRigSlotResolver
{
    RectTransform Resolve(CharRigSlot slot, bool strict);
}
