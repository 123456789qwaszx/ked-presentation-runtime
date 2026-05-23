using UnityEngine;

public static class PresentationShotIntentMath
{
    public const float MinZoom = -10f;
    public const float MaxZoom = 10f;

    public static float ClampZoom(float zoom)
    {
        return Mathf.Clamp(zoom, MinZoom, MaxZoom);
    }

    public static float EvaluateScale(float zoom)
    {
        float z = ClampZoom(zoom);
        return Mathf.Max(0.0001f, 1f + z * 0.05f);
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