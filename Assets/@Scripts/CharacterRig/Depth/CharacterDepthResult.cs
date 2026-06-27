using UnityEngine;

public readonly struct CharacterDepthResult
{
    public readonly Vector2 RawDepthYAnchoredPosition;

    public readonly Vector2 DepthScale;

    public readonly CharacterFocusPreset PreserveFocusPreset;
    public readonly Vector2 PreserveFocusOffset;

    public CharacterDepthResult(
        Vector2 rawDepthYAnchoredPosition,
        Vector2 depthScale,
        CharacterFocusPreset preserveFocusPreset,
        Vector2 preserveFocusOffset)
    {
        RawDepthYAnchoredPosition = rawDepthYAnchoredPosition;
        DepthScale = depthScale;

        PreserveFocusPreset = preserveFocusPreset;
        PreserveFocusOffset = preserveFocusOffset;
    }
}