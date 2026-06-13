using UnityEngine;

public readonly struct CharacterDepthResult
{
    // depth preset/level/tuning이 말하는 원래 목표 DepthY.
    // FocusPoint 보존 보정은 아직 더하지 않은 값이다.
    public readonly Vector2 RawDepthYAnchoredPosition;

    public readonly Vector2 DepthScale;

    public readonly CharacterFocusPreset PreserveFocusPreset;
    public readonly string PreserveCustomFocusKey;
    public readonly Vector2 PreserveFocusOffset;

    public CharacterDepthResult(
        Vector2 rawDepthYAnchoredPosition,
        Vector2 depthScale,
        CharacterFocusPreset preserveFocusPreset,
        string preserveCustomFocusKey,
        Vector2 preserveFocusOffset)
    {
        RawDepthYAnchoredPosition = rawDepthYAnchoredPosition;
        DepthScale = depthScale;

        PreserveFocusPreset = preserveFocusPreset;
        PreserveCustomFocusKey = preserveCustomFocusKey ?? "";
        PreserveFocusOffset = preserveFocusOffset;
    }
}