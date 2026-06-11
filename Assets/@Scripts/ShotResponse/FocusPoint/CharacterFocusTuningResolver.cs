using UnityEngine;

public static class CharacterFocusTuningResolver
{
    public static Vector2 ResolveOffset(
        CharacterFocusTuningDBSO tuningDb,
        string tuningKey,
        CharacterFocusPreset preset,
        string customPointKey,
        Vector2 commandOffset)
    {
        Vector2 offset = Vector2.zero;

        if (tuningDb != null)
            offset += tuningDb.baseOffsets.Get(preset);
        else
            offset += CharacterFocusOffsetSet.Default.Get(preset);

        CharacterFocusTuningDBSO.Entry entry = null;

        if (tuningDb != null)
            tuningDb.TryGet(tuningKey, out entry);

        if (entry != null)
        {
            offset += entry.defaultOffset;

            if (preset == CharacterFocusPreset.Custom)
            {
                if (entry.TryGetCustomPoint(customPointKey, out Vector2 customOffset))
                    offset += customOffset;
            }
            else
            {
                offset += entry.offsets.Get(preset);
            }
        }

        offset += commandOffset;

        return offset;
    }
}