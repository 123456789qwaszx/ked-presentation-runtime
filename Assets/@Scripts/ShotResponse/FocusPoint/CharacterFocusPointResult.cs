using UnityEngine;

public struct CharacterFocusPointResult
{
    public RectTransform StageRoot;

    // Focus calculation basis. Usually Character_CastTransform.
    public RectTransform FocusRect;

    // Marker display parent. Usually Character_ExtensionsRoot.
    public RectTransform PreviewRoot;

    // FocusRect local offset after preset/db/command correction.
    public Vector2 FocusOffsetInFocusRectSpace;

    // Final focus estimate in StageRoot local space.
    public Vector2 FocusPointInStageSpace;
}