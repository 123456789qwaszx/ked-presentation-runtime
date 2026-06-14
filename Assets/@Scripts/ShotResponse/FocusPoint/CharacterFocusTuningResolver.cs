using UnityEngine;

public static class CharacterFocusTuningResolver
{
    public static Vector2 ResolveOffset(
        CharacterFocusTuningDBSO tuningDb,
        string tuningKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset)
    {
        Vector2 offset = Vector2.zero;

        offset += tuningDb.baseOffsets.Get(preset);

        if (tuningDb.TryGet(tuningKey, out CharacterFocusTuningDBSO.Entry entry))
        {
            offset += entry.defaultOffset;
            offset += entry.offsets.Get(preset);
        }

        offset += commandOffset;

        return offset;
    }
}