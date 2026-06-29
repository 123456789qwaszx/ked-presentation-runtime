using UnityEngine;

public readonly struct CharacterVisualTuningResult
{
    public readonly Vector2 Offset;
    public readonly float Scale;

    public CharacterVisualTuningResult(Vector2 offset, float scale)
    {
        Offset = offset;
        Scale = scale <= 0f ? 1f : scale;
    }
}

public static class CharacterVisualTuningResolver
{
    public static CharacterVisualTuningResult Resolve(
        CharacterVisualTuningDBSO tuningDb,
        string tuningKey,
        Vector2 commandOffset,
        float commandScaleMultiplier)
    {
        Vector2 offset = Vector2.zero;
        float scale = 1f;

        if (tuningDb != null && tuningDb.TryGet(tuningKey, out var entry))
        {
            offset += entry.defaultOffset;
            scale *= Mathf.Max(0.0001f, entry.defaultScale);
        }

        offset += commandOffset;

        if (commandScaleMultiplier > 0f)
            scale *= commandScaleMultiplier;

        return new CharacterVisualTuningResult(offset, scale);
    }
}