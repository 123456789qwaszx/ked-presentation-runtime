using UnityEngine;

public static class PresentationResponseSolver
{
    private const float ZoomIntentScale = 0.1f;

    public static PresentationResponse Solve(
        in PresentationIntentState state,
        PresentationResponseProfile profile)
    {
        float zoom01 = Mathf.Clamp(state.zoom * ZoomIntentScale, -1f, 1f);

        float scaleMul = 1f + zoom01 * profile.maxZoomScaleDelta;
        Vector2 finalScale = profile.baseScale * Mathf.Max(0.01f, scaleMul);

        Vector2 spreadOffset = Vector2.zero;
        Vector2 fromFocus = profile.basePositionInRigSpace - state.focusPoint;

        if (profile.maxZoomSpreadPixels > 0f && fromFocus.sqrMagnitude > 0.0001f)
            spreadOffset = fromFocus.normalized * (zoom01 * profile.maxZoomSpreadPixels);

        Vector2 finalPosition =
            profile.basePositionInRigSpace
            + state.pan * profile.panResponse
            + spreadOffset;

        return new PresentationResponse
        {
            anchoredPosition = finalPosition,
            scale = finalScale,
            alpha = profile.baseAlpha,
        };
    }

    public static PresentationIntentState Lerp(
        in PresentationIntentState from,
        in PresentationIntentState to,
        float t)
    {
        return new PresentationIntentState
        {
            zoom = Mathf.Lerp(from.zoom, to.zoom, t),
            pan = Vector2.Lerp(from.pan, to.pan, t),
            focusPoint = Vector2.Lerp(from.focusPoint, to.focusPoint, t),
        };
    }
}