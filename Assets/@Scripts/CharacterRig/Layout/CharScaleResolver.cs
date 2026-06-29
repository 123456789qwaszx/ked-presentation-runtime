using UnityEngine;

public static class CharScaleResolver
{
    public static float ResolveScale(RoleAnchorTuningDBSO roleTuningDb, string roleKey)
    {
        float scale = 1f;

        if (roleTuningDb != null && roleTuningDb.TryGet(roleKey, out var entry))
        {
            scale *= Mathf.Max(0.0001f, entry.defaultScale);
            scale *= Mathf.Max(0.0001f, entry.visualScale);
        }

        return scale;
    }
}