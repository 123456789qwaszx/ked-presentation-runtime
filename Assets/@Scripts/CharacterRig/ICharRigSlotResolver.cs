using UnityEngine;

public interface ICharRigSlotResolver
{
    bool TryResolve(CharRigSlot slot, out RectTransform rect);
}