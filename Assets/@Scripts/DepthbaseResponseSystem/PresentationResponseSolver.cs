using UnityEngine;

/// <summary>
/// Intent + Profile → Response 계산기.
/// Unity Object에 의존하지 않는 순수 계산 레이어.
/// </summary>
public static class PresentationResponseSolver
{
    private const float ZoomIntentToNormalized = 0.1f;

    public static PresentationResponse Solve(PresentationIntentState intent, PresentationResponseProfile profile)
    {
        float zoom01 = Mathf.Clamp(intent.zoom * ZoomIntentToNormalized, -1f, 1f);

        float scaleFactor = 1f + zoom01 * profile.maxZoomScaleDelta;
        if (scaleFactor < 0.01f)
            scaleFactor = 0.01f;

        Vector2 spreadOffset = ComputeSpreadOffset(
            profile.basePositionInRigSpace,
            intent.focusPoint,
            zoom01,
            profile.maxZoomSpreadPixels);

        Vector2 finalPosition =
            profile.basePositionInRigSpace +
            spreadOffset +
            (intent.pan * profile.panResponse);

        return new PresentationResponse
        {
            positionInRigSpace = finalPosition,
            scale = profile.baseScale * scaleFactor,
            alpha = profile.baseAlpha,
        };
    }

    public static PresentationIntentState Lerp(PresentationIntentState from, PresentationIntentState to, float t)
    {
        return new PresentationIntentState
        {
            zoom = Mathf.Lerp(from.zoom, to.zoom, t),
            pan = Vector2.Lerp(from.pan, to.pan, t),
            focusPoint = Vector2.Lerp(from.focusPoint, to.focusPoint, t),
        };
    }

    private static Vector2 ComputeSpreadOffset(
        Vector2 basePositionInRigSpace,
        Vector2 focusPoint,
        float zoom01,
        float maxZoomSpreadPixels)
    {
        if (Mathf.Approximately(maxZoomSpreadPixels, 0f))
            return Vector2.zero;

        Vector2 fromFocus = basePositionInRigSpace - focusPoint;
        if (fromFocus.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        Vector2 direction = fromFocus.normalized;
        float distance = maxZoomSpreadPixels * zoom01;
        return direction * distance;
    }
}
