using System;
using UnityEngine;

public enum CharScalePreset
{
    None = 0,

    Normal = 1,
    Small = 2,
    Large = 3,

    Far = 10,
    Close = 11,

    Exp1 = 100,
    Exp2 = 101
}

[Serializable]
public struct CharScaleTuningSet
{
    public float normal;
    public float small;
    public float large;

    public float far;
    public float close;

    public float exp1;
    public float exp2;

    public float Get(CharScalePreset preset)
    {
        float value = preset switch
        {
            CharScalePreset.None => 1f,

            CharScalePreset.Normal => normal,
            CharScalePreset.Small => small,
            CharScalePreset.Large => large,

            CharScalePreset.Far => far,
            CharScalePreset.Close => close,

            CharScalePreset.Exp1 => exp1,
            CharScalePreset.Exp2 => exp2,

            _ => 1f,
        };

        return value <= 0f ? 1f : value;
    }

    public static CharScaleTuningSet Default => new()
    {
        normal = 1f,
        small = 0.9f,
        large = 1.1f,

        far = 0.85f,
        close = 1.2f,

        exp1 = 1f,
        exp2 = 1f
    };
}

public static class CharScaleResolver
{
    public static float ResolveScale(
        CharScalePreset preset,
        CharStageTuningSO stageTuning,
        RoleAnchorTuningDBSO roleTuningDb,
        string roleKey,
        float commandMultiplier)
    {
        float scale = 1f;

        RoleAnchorTuningDBSO.Entry roleEntry = null;

        if (roleTuningDb != null)
            roleTuningDb.TryGet(roleKey, out roleEntry);

        if (roleEntry != null)
            scale *= Mathf.Max(0.0001f, roleEntry.defaultScale);

        if (stageTuning != null)
            scale *= stageTuning.scales.Get(preset);

        if (roleEntry != null)
            scale *= roleEntry.scales.Get(preset);

        if (commandMultiplier > 0f)
            scale *= commandMultiplier;

        return scale;
    }
}