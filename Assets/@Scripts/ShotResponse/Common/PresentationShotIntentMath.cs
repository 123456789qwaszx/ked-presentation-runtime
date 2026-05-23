using UnityEngine;

public static class PresentationShotIntentMath
{
    public const float MinZoom = -10f;
    public const float MaxZoom = 10f;
    public const float DefaultZoomToScaleFactor = 0.05f;

    public static float ClampZoom(float zoom)
    {
        return Mathf.Clamp(zoom, MinZoom, MaxZoom);
    }

    public static float EvaluateCameraScale(float zoom, float maxScale = 5.0f)
    {
        float z = ClampZoom(zoom);
        float scale = Mathf.Max(0.0001f, 1f + z * DefaultZoomToScaleFactor);
        return Mathf.Min(scale, Mathf.Max(1.0001f, maxScale));
    }

    public static Vector2 ToLogicalFocusPoint(
        Vector2 focusPointInStageSpace,
        Vector2 currentPan,
        float currentScale)
    {
        float safeScale = Mathf.Max(0.0001f, currentScale);
        return (focusPointInStageSpace - currentPan) / safeScale;
    }

    public static Vector2 CalculatePanForFocus(
        Vector2 logicalFocusPointInStageSpace,
        Vector2 desiredPointInStageSpace,
        float targetScale)
    {
        return desiredPointInStageSpace - logicalFocusPointInStageSpace * targetScale;
    }

    public static PresentationIntentState Interpolate(
        in PresentationIntentState from,
        in PresentationIntentState to,
        float t)
    {
        float u = Mathf.Clamp01(t);

        return new PresentationIntentState
        {
            zoom = Mathf.Lerp(from.zoom, to.zoom, u),
            pan = Vector2.Lerp(from.pan, to.pan, u),
            focusPoint = Vector2.Lerp(from.focusPoint, to.focusPoint, u),
        };
    }

    public static bool ApproximatelyEqual(
        in PresentationIntentState a,
        in PresentationIntentState b)
    {
        return Mathf.Abs(a.zoom - b.zoom) <= 0.0001f &&
               Vector2.SqrMagnitude(a.pan - b.pan) <= 0.0001f &&
               Vector2.SqrMagnitude(a.focusPoint - b.focusPoint) <= 0.0001f;
    }
}

public static class PresentationResponseMath
{
    public static PresentationResponseBinding.Response SolveResponse(
        in PresentationIntentState state,
        PresentationResponseProfile profile)
    {
        float zoomFactor = PresentationShotIntentMath.ClampZoom(state.zoom);

        float scaleMultiplier = 1f + zoomFactor * profile.maxZoomScaleDelta;
        Vector2 scaledLocalScale = profile.baseLocalScale * Mathf.Max(0.01f, scaleMultiplier);

        Vector2 focusToTarget = profile.basePositionInRigSpace - state.focusPoint;

        Vector2 zoomSpreadOffset = CalculateZoomSpreadOffset(
            focusToTarget,
            zoomFactor,
            profile.maxZoomSpreadPixels);

        Vector2 finalPosition =
            profile.basePositionInRigSpace +
            state.pan * profile.panResponse +
            zoomSpreadOffset;

        return new PresentationResponseBinding.Response
        {
            anchoredPosition = finalPosition,
            scale = scaledLocalScale,
        };
    }

    public static Vector2 CalculateZoomSpreadOffset(
        Vector2 focusToTarget,
        float zoomFactor,
        float maxZoomSpreadPixels)
    {
        if (maxZoomSpreadPixels <= 0f)
            return Vector2.zero;

        if (focusToTarget.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        Vector2 spreadDirection = focusToTarget.normalized;
        float spreadDistance = zoomFactor * maxZoomSpreadPixels;

        return spreadDirection * spreadDistance;
    }
}

public static class PresentationCoordinateMath
{
    public static Vector2 ConvertPointFromRootToParentSpace(
        Vector2 pointInRootSpace,
        RectTransform root,
        RectTransform parent)
    {
        if (root == null || parent == null)
            return pointInRootSpace;

        if (ReferenceEquals(root, parent))
            return pointInRootSpace;

        Vector3 worldPosition = root.TransformPoint(
            new Vector3(pointInRootSpace.x, pointInRootSpace.y, 0f));

        Vector3 positionInParentSpace = parent.InverseTransformPoint(worldPosition);

        return new Vector2(positionInParentSpace.x, positionInParentSpace.y);
    }

    public static Vector2 MeasurePivotInRootSpace(
        RectTransform measureRect,
        RectTransform root)
    {
        if (measureRect == null || root == null)
            return Vector2.zero;

        Vector3 worldPivot = measureRect.TransformPoint(Vector3.zero);
        Vector3 localPivot = root.InverseTransformPoint(worldPivot);

        return new Vector2(localPivot.x, localPivot.y);
    }
}