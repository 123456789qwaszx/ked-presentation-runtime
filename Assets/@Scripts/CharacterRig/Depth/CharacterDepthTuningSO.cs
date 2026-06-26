using System;
using UnityEngine;

[Serializable]
public struct CharacterDepthPresetValue
{
    [Header("Depth Transform")]
    public Vector2 depthY;

    public float depthScale;

    [Header("Focus-Preserving Scale Pivot")]
    public CharacterFocusPreset preserveFocusPreset;

    public string preserveCustomFocusKey;

    public Vector2 preserveFocusOffset;

    public static CharacterDepthPresetValue Far => new()
    {
        depthY = new Vector2(0f, 480f),
        depthScale = 0.68f,
        preserveFocusPreset = CharacterFocusPreset.Feet,
        preserveCustomFocusKey = "",
        preserveFocusOffset = Vector2.zero,
    };
    
    public static CharacterDepthPresetValue Back => new()
    {
        depthY = new Vector2(0f, 240f),
        depthScale = 0.86f,
        preserveFocusPreset = CharacterFocusPreset.Bust,
        preserveCustomFocusKey = "",
        preserveFocusOffset = Vector2.zero,
    };

    public static CharacterDepthPresetValue Mid => new()
    {
        depthY = Vector2.zero,
        depthScale = 1f,
        preserveFocusPreset = CharacterFocusPreset.Bust,
        preserveCustomFocusKey = "",
        preserveFocusOffset = Vector2.zero,
    };

    public static CharacterDepthPresetValue Front => new()
    {
        depthY = new Vector2(0f, -320f),
        depthScale = 1.18f,
        preserveFocusPreset = CharacterFocusPreset.Bust,
        preserveCustomFocusKey = "",
        preserveFocusOffset = Vector2.zero,
    };
    
    public static CharacterDepthPresetValue Close => new()
    {
        depthY = new Vector2(0f, 440f),
        depthScale = 1.38f,
        preserveFocusPreset = CharacterFocusPreset.Face,
        preserveCustomFocusKey = "",
        preserveFocusOffset = Vector2.zero,
    };

}

[Serializable]
public struct CharacterDepthTuningSet
{
    public CharacterDepthPresetValue far;
    public CharacterDepthPresetValue back;
    public CharacterDepthPresetValue mid;
    public CharacterDepthPresetValue front;
    public CharacterDepthPresetValue close;

    public CharacterDepthPresetValue exp1;
    public CharacterDepthPresetValue exp2;

    public CharacterDepthPresetValue Get(CharacterDepthPreset preset) => preset switch
    {
        CharacterDepthPreset.None => mid,

        CharacterDepthPreset.Far => far,
        CharacterDepthPreset.Back => back,
        CharacterDepthPreset.Mid => mid,
        CharacterDepthPreset.Front => front,
        CharacterDepthPreset.Close => close,

        CharacterDepthPreset.Exp1 => exp1,
        CharacterDepthPreset.Exp2 => exp2,

        _ => mid,
    };

    public static CharacterDepthTuningSet Default => new()
    {
        far = CharacterDepthPresetValue.Far,
        back = CharacterDepthPresetValue.Back,
        mid = CharacterDepthPresetValue.Mid,
        front = CharacterDepthPresetValue.Front,
        close = CharacterDepthPresetValue.Close,

        exp1 = CharacterDepthPresetValue.Mid,
        exp2 = CharacterDepthPresetValue.Mid,
    };
}

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Character Depth Tuning", fileName = "CharacterDepthTuning")]
public sealed class CharacterDepthTuningSO : ScriptableObject
{
    [Header("Preset Values")]
    public CharacterDepthTuningSet presets = CharacterDepthTuningSet.Default;

    [Header("Numeric Level")]
    [Tooltip("depth level에 대한 Y 커브입니다. 키 범위 밖의 값은 끝 키의 기울기로 외삽됩니다.")]
    public AnimationCurve levelYCurve = AnimationCurve.Linear(0f, 120f, 10f, -440f);

    [Tooltip("depth level에 대한 Scale 커브입니다. 키 범위 밖의 값은 끝 키의 기울기로 외삽됩니다.")]
    public AnimationCurve levelScaleCurve = AnimationCurve.Linear(0f, 0.86f, 10f, 1.38f);

    [Header("Numeric Level Focus Pivot")]
    public CharacterFocusPreset levelFarPreserveFocus = CharacterFocusPreset.Feet;
    public CharacterFocusPreset levelMidPreserveFocus = CharacterFocusPreset.Bust;
    public CharacterFocusPreset levelClosePreserveFocus = CharacterFocusPreset.Bust;
    public CharacterFocusPreset levelFrontPreserveFocus = CharacterFocusPreset.Face;

    public CharacterDepthPresetValue ResolvePreset(CharacterDepthPreset preset)
    {
        return presets.Get(preset);
    }

    public CharacterDepthPresetValue ResolveLevel(float level)
    {
        CharacterFocusPreset preserveFocus = ResolvePreserveFocusForLevel(level);

        float depthY = EvaluateCurveUnclamped(levelYCurve, level);
        float depthScale = EvaluateCurveUnclamped(levelScaleCurve, level);

        return new CharacterDepthPresetValue
        {
            depthY = new Vector2(0f, depthY),
            depthScale = Mathf.Max(0.0001f, depthScale),

            preserveFocusPreset = preserveFocus,
            preserveCustomFocusKey = "",
            preserveFocusOffset = Vector2.zero,
        };
    }

    private CharacterFocusPreset ResolvePreserveFocusForLevel(float level)
    {
        if (level <= 2.5f)
            return levelFarPreserveFocus;

        if (level <= 6.5f)
            return levelMidPreserveFocus;

        if (level <= 8.5f)
            return levelClosePreserveFocus;

        return levelFrontPreserveFocus;
    }

    private static float EvaluateCurveUnclamped(AnimationCurve curve, float time)
    {
        if (curve == null || curve.length <= 0)
            return 0f;

        if (curve.length == 1)
            return curve.keys[0].value;

        Keyframe first = curve.keys[0];
        Keyframe last = curve.keys[curve.length - 1];

        if (time < first.time)
        {
            Keyframe next = curve.keys[1];
            float slope = CalculateSlope(first, next);
            return first.value + slope * (time - first.time);
        }

        if (time > last.time)
        {
            Keyframe prev = curve.keys[curve.length - 2];
            float slope = CalculateSlope(prev, last);
            return last.value + slope * (time - last.time);
        }

        return curve.Evaluate(time);
    }

    private static float CalculateSlope(Keyframe a, Keyframe b)
    {
        float dt = b.time - a.time;

        if (Mathf.Abs(dt) <= 0.0001f)
            return 0f;

        return (b.value - a.value) / dt;
    }
}