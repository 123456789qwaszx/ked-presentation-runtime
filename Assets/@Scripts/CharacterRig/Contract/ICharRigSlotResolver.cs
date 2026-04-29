using UnityEngine;

public enum CharRigSlot
{
    CharacterStageSlot00 = 0,
    CharacterStageSlot01 = 1,
    CharacterStageSlot02 = 2,
    ProtagonistSlot = 3,
    LiveChatIdolSlot00 = 100,
    LiveChatIdolSlot01 = 101
}

public interface ICharRigSlotResolver
{
    RectTransform Resolve(CharRigSlot slot, bool strict);
}
