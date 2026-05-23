using UnityEngine;

public struct CharacterFocusPointResult
{
    public RectTransform StageRoot;
    public RectTransform FocusRect;

    public Vector2 FocusOffsetInFocusRectSpace;
    public Vector2 FocusPointInStageSpace;
}