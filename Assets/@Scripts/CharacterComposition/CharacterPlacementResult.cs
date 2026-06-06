using UnityEngine;

public readonly struct CharacterPlacementResult
{
    public readonly RectTransform MoveRect;
    public readonly Vector2 DestinationAnchoredPosition;

    public readonly Vector2 CurrentFocusInRigSpace;
    public readonly Vector2 DesiredFocusInRigSpace;
    public readonly Vector2 DeltaInTargetParentSpace;

    public CharacterPlacementResult(
        RectTransform moveRect,
        Vector2 destinationAnchoredPosition,
        Vector2 currentFocusInRigSpace,
        Vector2 desiredFocusInRigSpace,
        Vector2 deltaInTargetParentSpace)
    {
        MoveRect = moveRect;
        DestinationAnchoredPosition = destinationAnchoredPosition;
        CurrentFocusInRigSpace = currentFocusInRigSpace;
        DesiredFocusInRigSpace = desiredFocusInRigSpace;
        DeltaInTargetParentSpace = deltaInTargetParentSpace;
    }
}

public readonly struct CharacterPlacementScalePreview
{
    public readonly bool Enabled;
    public readonly RectTransform ScaleRect;
    public readonly Vector2 TargetScale;

    public CharacterPlacementScalePreview(
        RectTransform scaleRect,
        Vector2 targetScale)
    {
        Enabled = scaleRect != null;
        ScaleRect = scaleRect;
        TargetScale = targetScale;
    }

    public static CharacterPlacementScalePreview None =>
        new CharacterPlacementScalePreview(null, Vector2.one);
}

public static class CharacterPlacementSolver
{
    public static bool TryCalculateFocusPlacement(
        CommandRunScope scope,
        string roleKey,
        RectTransform moveRect,
        CharacterFocusPreset focusPreset,
        string poseKey,
        string customFocusKey,
        Vector2 focusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        ScreenFocusPoint screenPoint,
        Vector2 screenOffset,
        CharacterPlacementScalePreview scalePreview,
        out CharacterPlacementResult result)
    {
        result = default;

        if (scope == null)
            return false;

        if (moveRect == null)
            return false;

        RectTransform targetParent = moveRect.parent as RectTransform;
        if (targetParent == null)
            return false;

        Vector3 originalScale = Vector3.one;
        bool didPreviewScale = false;

        if (scalePreview.Enabled && scalePreview.ScaleRect != null)
        {
            originalScale = scalePreview.ScaleRect.localScale;

            Vector3 preview = originalScale;
            preview.x = scalePreview.TargetScale.x;
            preview.y = scalePreview.TargetScale.y;

            scalePreview.ScaleRect.localScale = preview;
            didPreviewScale = true;
        }

        bool resolved = CharacterFocusPointResolver.TryResolve(
            scope,
            roleKey,
            focusPreset,
            poseKey,
            customFocusKey,
            focusOffset,
            focusTuningDb,
            out CharacterFocusPointResult focus);

        if (didPreviewScale)
            scalePreview.ScaleRect.localScale = originalScale;

        if (!resolved)
            return false;

        if (focus.StageRoot == null)
            return false;

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

        Vector2 deltaInParentSpace = desiredFocusInParentSpace - currentFocusInParentSpace;
        Vector2 destination = moveRect.anchoredPosition + deltaInParentSpace;

        result = new CharacterPlacementResult(
            moveRect,
            destination,
            focus.FocusPointInStageSpace,
            desiredFocusInRigSpace,
            deltaInParentSpace);

        return true;
    }
}