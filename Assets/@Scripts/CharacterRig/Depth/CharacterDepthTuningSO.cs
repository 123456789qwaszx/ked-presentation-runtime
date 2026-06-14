using System;
using UnityEngine;

[Serializable]
public struct CharacterDepthPresetValue
{
    [Header("Depth Transform")]
    // CharSlot_DepthY에 적용될 기준 위치입니다. 보통 X는 0, Y만 사용.
    public Vector2 depthY;

    //CharSlot_DepthScale에 적용될 기준 배율.
    public float depthScale;

    [Header("Focus-Preserving Scale Pivot")]
    //DepthScale이 이 FocusPoint를 기준으로 scale되는 것처럼 DepthY 보정을 계산합니다.
    public CharacterFocusPreset preserveFocusPreset;

    // preserveFocusPreset이 Custom일 때 사용할 custom point key.
    public string preserveCustomFocusKey;

    // preserveFocusPreset에 추가로 더할 command/global offset.
    public Vector2 preserveFocusOffset;

    public static CharacterDepthPresetValue Far => new()
    {
        depthY = new Vector2(0f, 120f),
        depthScale = 0.86f,
        preserveFocusPreset = CharacterFocusPreset.Feet,
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

    public static CharacterDepthPresetValue Close => new()
    {
        depthY = new Vector2(0f, -320f),
        depthScale = 1.22f,
        preserveFocusPreset = CharacterFocusPreset.Bust,
        preserveCustomFocusKey = "",
        preserveFocusOffset = Vector2.zero,
    };

    public static CharacterDepthPresetValue Front => new()
    {
        depthY = new Vector2(0f, -440f),
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
    public CharacterDepthPresetValue mid;
    public CharacterDepthPresetValue close;
    public CharacterDepthPresetValue front;

    public CharacterDepthPresetValue exp1;
    public CharacterDepthPresetValue exp2;

    public CharacterDepthPresetValue Get(CharacterDepthPreset preset) => preset switch
    {
        CharacterDepthPreset.None => mid,

        CharacterDepthPreset.Far => far,
        CharacterDepthPreset.Mid => mid,
        CharacterDepthPreset.Close => close,
        CharacterDepthPreset.Front => front,

        CharacterDepthPreset.Exp1 => exp1,
        CharacterDepthPreset.Exp2 => exp2,

        _ => mid,
    };

    public static CharacterDepthTuningSet Default => new()
    {
        far = CharacterDepthPresetValue.Far,
        mid = CharacterDepthPresetValue.Mid,
        close = CharacterDepthPresetValue.Close,
        front = CharacterDepthPresetValue.Front,

        exp1 = CharacterDepthPresetValue.Mid,
        exp2 = CharacterDepthPresetValue.Mid,
    };
}

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Character Depth Tuning", fileName = "CharacterDepthTuning")]
public sealed class CharacterDepthTuningSO : ScriptableObject
{
    [Header("Preset Values")]
    public CharacterDepthTuningSet presets = CharacterDepthTuningSet.Default;

    [Header("Numeric Level 0~10")]
    [Tooltip("depth level 0~10에 대한 Y 커브입니다. x=level, y=CharSlot_DepthY.y")]
    public AnimationCurve levelYCurve = AnimationCurve.Linear(0f, 120f, 10f, -440f);

    [Tooltip("depth level 0~10에 대한 Scale 커브입니다. x=level, y=CharSlot_DepthScale uniform scale")]
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
        float clampedLevel = Mathf.Clamp(level, 0f, 10f);

        CharacterFocusPreset preserveFocus = ResolvePreserveFocusForLevel(clampedLevel);

        return new CharacterDepthPresetValue
        {
            depthY = new Vector2(0f, levelYCurve.Evaluate(clampedLevel)),

            depthScale =Mathf.Max(0.0001f, levelScaleCurve.Evaluate(clampedLevel)),

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
}