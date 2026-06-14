using UnityEngine;

public static class CharacterFocusPlacementSolver
{
    public static bool TryCalculateFocusPlacement(
        CommandRunScope scope,
        string roleKey,
        RectTransform moveRect,
        CharacterFocusPreset focusPreset,
        Vector2 focusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        ScreenFocusPoint screenPoint,
        Vector2 screenOffset,
        out Vector2 destinationAnchoredPosition)
    {
        destinationAnchoredPosition = default;

        if (moveRect == null)
            return false;

        RectTransform targetParent = moveRect.parent as RectTransform;

        if (targetParent == null)
            return false;

        CharacterFocusPointResolver.TryResolve(
            scope,
            roleKey,
            focusPreset,
            focusOffset,
            focusTuningDb,
            useSettledPlacementTargets: true,
            out CharacterFocusPointResult focus);

        Vector2 desiredFocusInRigSpace =
            ScreenFocusPointResolver.Resolve(focus.StageRoot, screenPoint) + screenOffset;

        Vector2 currentFocusInParentSpace =
            PresentationCoordinateMath.ConvertPointFromRigSpaceToTargetPositionParentSpace(
                focus.FocusPointInStageSpace,
                focus.StageRoot,
                targetParent);

        Vector2 desiredFocusInParentSpace =
            PresentationCoordinateMath.ConvertPointFromRigSpaceToTargetPositionParentSpace(
                desiredFocusInRigSpace,
                focus.StageRoot,
                targetParent);

        Vector2 deltaInParentSpace =
            desiredFocusInParentSpace - currentFocusInParentSpace;

        destinationAnchoredPosition =
            moveRect.anchoredPosition + deltaInParentSpace;

        return true;
    }
}