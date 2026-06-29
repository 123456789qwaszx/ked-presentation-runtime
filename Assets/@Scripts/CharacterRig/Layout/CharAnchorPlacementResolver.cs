using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

// Final composition resolution logic.
public static class CharAnchorPlacementResolver
{
    public static Vector2 ResolveAnchoredPosition(RoleAnchorTuningDBSO roleTuningDb, string roleKey)
    {
        Vector2 pos = new Vector2(0f, 0f);

        if (roleTuningDb != null && roleTuningDb.TryGet(roleKey, out var entry))
        {
            pos += entry.defaultOffset;
            pos += entry.offset;
        }

        // 7) Return the final anchored position.
        return pos;
    }
}