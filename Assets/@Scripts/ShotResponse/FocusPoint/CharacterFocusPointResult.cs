using UnityEngine;

public struct CharacterFocusPointResult
{
    public RectTransform StageRoot;

    // Final focus estimate in StageRoot local space.
    public Vector2 FocusPointInStageSpace;
}